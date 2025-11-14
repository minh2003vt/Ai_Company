using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Google.Cloud.Firestore;
using Infrastructure.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Service
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly QdrantService _qdrantService;
        private readonly FirebaseService _firebaseService;
        private readonly IGeminiService _geminiService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ChatService(
            IUnitOfWork unitOfWork,
            QdrantService qdrantService,
            FirebaseService firebaseService,
            IGeminiService geminiService,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _qdrantService = qdrantService;
            _firebaseService = firebaseService;
            _geminiService = geminiService;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<ChatResponseDto>> ProcessChatAsync(ChatRequestDto request, Guid userId)
        {
            try
            {
                // 1. Get chat session first
                var chatSession = await _unitOfWork.ChatSessions.GetByIdAsync(request.ChatSessionId);
                if (chatSession == null)
                {
                    return ApiResponse<ChatResponseDto>.Fail(null, "Không tìm thấy phiên chat");
                }

                // 2. Validate AI Configure and user access
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(chatSession.AIConfigureId);
                if (aiConfigure == null)
                {
                    return ApiResponse<ChatResponseDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                // Check if user has access to this AI
                var hasAccess = await CheckUserAccessToAI(userId, chatSession.AIConfigureId);
                if (!hasAccess)
                {
                    return ApiResponse<ChatResponseDto>.Fail(null, "Bạn không có quyền sử dụng AI này");
                }

                // 3. Làm sạch query và Build RAG context via Qdrant semantic search (embed query → top K chunks)
                var cleanedQuery = CleanQuery(request.Text);
                // Lấy số chunks từ AI_Configure.RagTopK (default 10), giới hạn tối đa 10
                var configuredTopK = aiConfigure.RagTopK > 0 ? aiConfigure.RagTopK : 10;
                var topK = Math.Min(configuredTopK, 10);
                var ragContext = await BuildQdrantRagContext(chatSession.AIConfigureId, cleanedQuery, topK);
                // Rules không còn thêm vào ragContext nữa, sẽ được gửi qua system_instruction trong GeminiService

                // Debug: Log RAG context
                Console.WriteLine($"[ChatService] RAG Context - RetrievedChunks count: {ragContext.RetrievedChunks.Count}");
                if (ragContext.RetrievedChunks.Any())
                {
                    Console.WriteLine($"[ChatService] First chunk: {ragContext.RetrievedChunks.First().Content.Substring(0, Math.Min(100, ragContext.RetrievedChunks.First().Content.Length))}...");
                }
                else
                {
                    Console.WriteLine($"[ChatService] WARNING: No RAG chunks retrieved for AIConfigureId: {chatSession.AIConfigureId}");
                }

                // Load ModelConfig
                var modelConfig = await _unitOfWork.AIModelConfigs.GetByIdAsync(aiConfigure.ModelConfigId);
                if (modelConfig == null)
                {
                    return ApiResponse<ChatResponseDto>.Fail(null, "ModelConfig không tồn tại");
                }

                // Không dùng conversation history, chỉ đẩy chunks vào
                var emptyHistory = new List<ConversationContext>();
                var contextWithChunks = BuildGeminiContext(request.Text, ragContext, emptyHistory);
                
                // Debug: Log final context
                Console.WriteLine($"[ChatService] Final context length: {contextWithChunks.Length}");
                var previewLength = Math.Min(1000, contextWithChunks.Length);
                Console.WriteLine($"[ChatService] Final context preview (first {previewLength} chars):\n{contextWithChunks.Substring(0, previewLength)}");
                
                // Check if RAG is in context
                var hasRagInContext = contextWithChunks.Contains("retrievedChunks") || contextWithChunks.Contains("Rag");
                Console.WriteLine($"[ChatService] RAG in context: {hasRagInContext}");
                
                var geminiResponse = await _geminiService.GenerateResponseAsync(contextWithChunks, aiConfigure, modelConfig);

                // 7. Save to Firebase
                await SaveMessageToFirebase(chatSession.ExternalSessionId, userId, request.Text, "user");
                await SaveMessageToFirebase(chatSession.ExternalSessionId, Guid.Empty, geminiResponse, "assistant");

                // 8. Return response
                var response = new ChatResponseDto
                {
                    Response = geminiResponse,
                    RetrievedChunks = ragContext.RetrievedChunks,
                    SessionId = chatSession.ExternalSessionId,
                    Timestamp = DateTime.UtcNow
                };

                return ApiResponse<ChatResponseDto>.Ok(response, "Xử lý chat thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatResponseDto>.Fail(null, $"Lỗi khi xử lý chat: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ChatSessionDto>>> GetUserChatSessionsAsync(Guid userId)
        {
            try
            {
                var sessions = await _unitOfWork.ChatSessions.FindAsync(cs => cs.UserId == userId);
                var result = new List<ChatSessionDto>();

                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(session.AIConfigureId);
                    result.Add(new ChatSessionDto
                    {
                        Id = session.Id,
                        AIConfigureId = session.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        Title = session.Title,
                        CreatedAt = session.CreatedAt,
                        ExternalSessionId = session.ExternalSessionId
                    });
                }

                return ApiResponse<List<ChatSessionDto>>.Ok(result, "Lấy danh sách phiên chat thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ChatSessionDto>>.Fail(null, $"Lỗi khi lấy danh sách phiên chat: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ChatSessionDto>>> GetUserChatSessionsByAIAsync(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var sessions = await _unitOfWork.ChatSessions.FindAsync(cs => cs.UserId == userId && cs.AIConfigureId == aiConfigureId);
                var result = new List<ChatSessionDto>();

                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(session.AIConfigureId);
                    result.Add(new ChatSessionDto
                    {
                        Id = session.Id,
                        AIConfigureId = session.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        Title = session.Title,
                        CreatedAt = session.CreatedAt,
                        ExternalSessionId = session.ExternalSessionId
                    });
                }

                return ApiResponse<List<ChatSessionDto>>.Ok(result, "Lấy danh sách phiên chat theo AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ChatSessionDto>>.Fail(null, $"Lỗi khi lấy danh sách phiên chat: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ChatSessionDto>>> GetChatSessionsByUserIdAsync(Guid targetUserId)
        {
            try
            {
                // Check if target user exists
                var targetUser = await _unitOfWork.Users.GetByIdAsync(targetUserId);
                if (targetUser == null)
                {
                    return ApiResponse<List<ChatSessionDto>>.Fail(null, "Người dùng không tồn tại");
                }

                var sessions = await _unitOfWork.ChatSessions.FindAsync(cs => cs.UserId == targetUserId);
                var result = new List<ChatSessionDto>();

                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(session.AIConfigureId);
                    result.Add(new ChatSessionDto
                    {
                        Id = session.Id,
                        AIConfigureId = session.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        Title = session.Title,
                        CreatedAt = session.CreatedAt,
                        ExternalSessionId = session.ExternalSessionId
                    });
                }

                return ApiResponse<List<ChatSessionDto>>.Ok(result, $"Lấy danh sách phiên chat của người dùng {targetUser.FullName} thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ChatSessionDto>>.Fail(null, $"Lỗi khi lấy danh sách phiên chat: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ChatSessionDto>> CreateChatSessionAsync(Guid aiConfigureId, Guid userId, string? title = null)
        {
            try
            {
                // Check user access
                var hasAccess = await CheckUserAccessToAI(userId, aiConfigureId);
                if (!hasAccess)
                {
                    return ApiResponse<ChatSessionDto>.Fail(null, "Bạn không có quyền sử dụng AI này");
                }

                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
                if (aiConfigure == null)
                {
                    return ApiResponse<ChatSessionDto>.Fail(null, "Không tìm thấy cấu hình AI");
                }

                var chatSession = new ChatSession
                {
                    AIConfigureId = aiConfigureId,
                    UserId = userId,
                    Title = title ?? $"Chat với {aiConfigure.Name}",
                    ExternalSessionId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ChatSessions.AddAsync(chatSession);
                await _unitOfWork.SaveChangesAsync();

                // Create Firebase session
                var externalSessionId = await _firebaseService.CreateChatSessionAsync(chatSession);
                chatSession.ExternalSessionId = externalSessionId;
                _unitOfWork.ChatSessions.Update(chatSession);
                await _unitOfWork.SaveChangesAsync();

                var result = new ChatSessionDto
                {
                    Id = chatSession.Id,
                    AIConfigureId = chatSession.AIConfigureId,
                    AIConfigureName = aiConfigure.Name,
                    Title = chatSession.Title,
                    CreatedAt = chatSession.CreatedAt,
                    ExternalSessionId = chatSession.ExternalSessionId
                };

                return ApiResponse<ChatSessionDto>.Ok(result, "Tạo phiên chat thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<ChatSessionDto>.Fail(null, $"Lỗi khi tạo phiên chat: {ex.Message}");
            }
        }

        private async Task<bool> CheckUserAccessToAI(Guid userId, Guid aiConfigureId)
        {
            // Check if user created the AI
            var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
            if (aiConfigure?.CreatedByUserId == userId)
                return true;

            // Check if user has access via UserAiConfig
            return await _unitOfWork.UserAiConfigs.HasAccessAsync(userId, aiConfigureId);
        }

        private async Task<List<ConversationContext>> GetConversationHistory(string externalSessionId, int limit)
        {
            try
            {
                // Get messages from Firebase in the new format
                var messagesRef = _firebaseService.Db.Collection("chatSessions")
                    .Document(externalSessionId)
                    .Collection("messages");

                var snapshot = await messagesRef.OrderBy("timestamp")
                    .Limit(limit * 2) // Get more to pair user/assistant
                    .GetSnapshotAsync();

                var messages = snapshot.Documents
                    .Select(d => new { 
                        User = d.GetValue<string>("User"),
                        userName = d.GetValue<string>("userName"),
                        text = d.GetValue<string>("text"),
                        timestamp = d.GetValue<long>("timestamp")
                    })
                    .OrderBy(m => m.timestamp)
                    .ToList();

                var conversations = new List<ConversationContext>();

                for (int i = 0; i < messages.Count - 1; i += 2)
                {
                    if (i + 1 < messages.Count)
                    {
                        var userMsg = messages[i];
                        var assistantMsg = messages[i + 1];

                        conversations.Add(new ConversationContext
                        {
                            UserRequest = userMsg.text,
                            GeminiResponse = assistantMsg.text
                        });
                    }
                }

                return conversations.Take(limit).ToList();
            }
            catch
            {
                return new List<ConversationContext>();
            }
        }

        private async Task<RagContext> PerformRAGFromKnowledgeSource(Guid aiConfigureId, int topK)
        {
            try
            {
                // Get knowledge sources directly from database
                var knowledgeSources = await _unitOfWork.KnowledgeSources.FindAsync(ks => ks.AIConfigureId == aiConfigureId);
                
                var retrievedChunks = new List<RetrievedChunkDto>();
                
                // Take the first topK chunks (ordered by ChunkIndex)
                var selectedChunks = knowledgeSources
                    .OrderBy(ks => ks.ChunkIndex ?? 0)
                    .Take(topK)
                    .ToList();

                foreach (var ks in selectedChunks)
                {
                    retrievedChunks.Add(new RetrievedChunkDto
                    {
                        Id = ks.Id.ToString(),
                        Source = ks.Source ?? ks.Title ?? "Unknown",
                        Content = ks.Content ?? "",
                        Score = 1.0f // No similarity score when using direct database retrieval
                    });
                }

                return new RagContext { RetrievedChunks = retrievedChunks };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving from KnowledgeSource: {ex.Message}");
                return new RagContext();
            }
        }

        private async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                // Gọi embedding service trực tiếp từ configuration - không cần ONNX nữa
                var tokenizerBaseUrl = _configuration["Tokenizer:BaseUrl"] ?? "http://localhost:8000";
                var embedUrl = $"{tokenizerBaseUrl.TrimEnd('/')}/embed";
                var requestBody = new { text, max_length = 128 };
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(embedUrl, content);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var embedResponse = JsonSerializer.Deserialize<EmbedResponse>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                if (embedResponse == null || embedResponse.Embedding == null || embedResponse.Embedding.Length == 0)
                {
                    throw new InvalidOperationException("Embedding service returned empty or null embedding");
                }
                
                return embedResponse.Embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GenerateEmbeddingAsync] ERROR: Failed to generate embedding - {ex.Message}");
                return Array.Empty<float>();
            }
        }

        private class EmbedResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public int Dimension { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private string CleanQuery(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = System.Text.RegularExpressions.Regex.Replace(text, "\r\n?|\u000B|\u000C|\u0085|\u2028|\u2029", " \n ");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "\\s+", " ");

            return normalized.Trim();
        }

        private List<string> ExtractRules(string rulesText)
        {
            if (string.IsNullOrWhiteSpace(rulesText))
                return new List<string>();

            return rulesText.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(rule => rule.Trim())
                          .Where(rule => !string.IsNullOrWhiteSpace(rule))
                          .ToList();
        }

        private string BuildGeminiContext(string userQuery, RagContext ragContext, List<ConversationContext> history)
        {
            var context = new StringBuilder();

            // Build context chỉ với user query và RAG chunks (không có conversation history)
            var conversationData = new Dictionary<string, object>();

            // Add current conversation
            var currentConv = new Dictionary<string, object>
            {
                ["userRequest"] = new { text = userQuery }
            };

            // Add RAG context if available (Rules không còn ở đây nữa, đã chuyển sang system_instruction)
            if (ragContext.RetrievedChunks.Any())
            {
                Console.WriteLine($"[BuildGeminiContext] Adding {ragContext.RetrievedChunks.Count} RAG chunks to context");
                var ragData = new Dictionary<string, object>();
                
                ragData["retrievedChunks"] = ragContext.RetrievedChunks.Select(chunk => new
                {
                    id = chunk.Id,
                    source = chunk.Source,
                    content = chunk.Content
                }).ToList();

                currentConv["Rag"] = ragData;
            }
            else
            {
                Console.WriteLine($"[BuildGeminiContext] WARNING: No RAG chunks available - ragContext.RetrievedChunks is empty");
            }

            // Chỉ thêm current conversation, không có history
            conversationData["CurrentConversation"] = currentConv;

            // Convert to JSON format
            var jsonContext = System.Text.Json.JsonSerializer.Serialize(conversationData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            context.AppendLine("Context:");
            context.AppendLine(jsonContext);

            return context.ToString();
        }

        private async Task SaveMessageToFirebase(string externalSessionId, Guid userId, string content, string role)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                var userName = role == "user" ? (user?.FullName ?? "User") : "Gemini";
                var userIdString = role == "user" ? userId.ToString() : "Gemini";

                // Use the new method that saves in your exact format
                await _firebaseService.SaveChatMessageAsync(externalSessionId, userIdString, userName, content, role);
            }
            catch
            {
                // Best effort - don't fail the whole request if Firebase fails
            }
        }

        public async Task<ApiResponse<bool>> DeleteChatSessionAsync(Guid sessionId, Guid userId)
        {
            try
            {
                var chatSession = await _unitOfWork.ChatSessions.GetByIdAsync(sessionId);
                if (chatSession == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy phiên chat");
                }

                // Check if user owns this session
                if (chatSession.UserId != userId)
                {
                    return ApiResponse<bool>.Fail(false, "Bạn không có quyền xóa phiên chat này");
                }

                // Delete Firebase collection
                if (!string.IsNullOrEmpty(chatSession.ExternalSessionId))
                {
                    try
                    {
                        var sessionRef = _firebaseService.Db.Collection("chatSessions").Document(chatSession.ExternalSessionId);
                        await sessionRef.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the operation
                        Console.WriteLine($"Failed to delete Firebase session: {ex.Message}");
                    }
                }

                // Delete from database
                _unitOfWork.ChatSessions.Delete(chatSession);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa phiên chat thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa phiên chat: {ex.Message}");
            }
        }

        private string? GetPayloadValue(object? payload, string key)
        {
            if (payload == null) return null;
            
            try
            {
                var jsonElement = (System.Text.Json.JsonElement)payload;
                if (jsonElement.TryGetProperty(key, out var value))
                {
                    return value.GetString();
                }
            }
            catch
            {
                // Fallback to string representation
                return payload.ToString();
            }
            
            return null;
        }

        private async Task<RagContext> BuildFullKnowledgeSourceContext(Guid aiConfigureId)
        {
            var rag = new RagContext();
            try
            {
                var sources = await _unitOfWork.KnowledgeSources.FindAsync(ks => ks.AIConfigureId == aiConfigureId);
                var ordered = sources
                    .OrderBy(ks => ks.ChunkIndex ?? 0)
                    .ToList();

                if (ordered.Count == 0)
                {
                    return rag;
                }

                // Option A: provide all chunks individually
                foreach (var ks in ordered)
                {
                    rag.RetrievedChunks.Add(new RetrievedChunkDto
                    {
                        Id = ks.Id.ToString(),
                        Source = ks.Source ?? ks.Title ?? "Unknown",
                        Content = ks.Content ?? string.Empty,
                        Score = 1.0f
                    });
                }

                return rag;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BuildFullKnowledgeSourceContext error: {ex.Message}");
                return rag;
            }
        }

        private async Task<RagContext> BuildQdrantRagContext(Guid aiConfigureId, string queryText, int limit = 10)
        {
            var rag = new RagContext();
            try
            {
                Console.WriteLine($"[BuildQdrantRagContext] Starting RAG for AIConfigureId: {aiConfigureId}, query: {queryText.Substring(0, Math.Min(50, queryText.Length))}..., limit: {limit}");
                
                // 1) Embed user query
                var queryVector = await GenerateEmbeddingAsync(queryText);
                Console.WriteLine($"[BuildQdrantRagContext] Generated embedding, vector length: {queryVector?.Length ?? 0}");

                // Validate vector
                if (queryVector == null || queryVector.Length == 0)
                {
                    Console.WriteLine($"[BuildQdrantRagContext] ERROR: Invalid embedding vector (null or empty), falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }

                // 2) Search Qdrant for top-K relevant chunks
                var collectionName = aiConfigureId.ToString();
                var requestedLimit = limit > 0 ? limit : 10;
                var searchLimit = Math.Max(requestedLimit, 10);
                Console.WriteLine($"[BuildQdrantRagContext] Requesting up to {searchLimit} chunks from Qdrant (configured limit: {limit})");
                Console.WriteLine($"[BuildQdrantRagContext] Searching Qdrant collection: {collectionName}");
                
                // Check if collection exists first
                var collectionInfo = await _qdrantService.GetCollectionAsync(collectionName);
                if (string.IsNullOrWhiteSpace(collectionInfo))
                {
                    Console.WriteLine($"[BuildQdrantRagContext] WARNING: Collection '{collectionName}' does not exist in Qdrant, falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }

                // Log collection info for debugging
                Console.WriteLine($"[BuildQdrantRagContext] Collection info: {collectionInfo.Substring(0, Math.Min(500, collectionInfo.Length))}...");

                // Validate vector dimension matches collection config
                int? collectionVectorSize = null;
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(collectionInfo);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("result", out var result) && 
                        result.TryGetProperty("config", out var config) &&
                        config.TryGetProperty("params", out var @params) &&
                        @params.TryGetProperty("vectors", out var vectors))
                    {
                        // Check if it's named vectors or single vector config
                        if (vectors.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            // Try named vectors first
                            if (vectors.TryGetProperty("named", out var named))
                            {
                                Console.WriteLine($"[BuildQdrantRagContext] Collection uses named vectors, checking first vector config...");
                                // Get first named vector config
                                foreach (var prop in named.EnumerateObject())
                                {
                                    if (prop.Value.TryGetProperty("size", out var sizeElement))
                                    {
                                        collectionVectorSize = sizeElement.GetInt32();
                                        Console.WriteLine($"[BuildQdrantRagContext] Named vector '{prop.Name}' has size: {collectionVectorSize}");
                                        break;
                                    }
                                }
                            }
                            // Try single vector config
                            else if (vectors.TryGetProperty("size", out var sizeElement))
                            {
                                collectionVectorSize = sizeElement.GetInt32();
                                Console.WriteLine($"[BuildQdrantRagContext] Collection vector size: {collectionVectorSize}");
                            }
                        }

                        if (collectionVectorSize.HasValue && queryVector.Length != collectionVectorSize.Value)
                        {
                            Console.WriteLine($"[BuildQdrantRagContext] ERROR: Vector dimension mismatch! Query vector: {queryVector.Length}, Collection expects: {collectionVectorSize.Value}, falling back to database retrieval...");
                            return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                        }
                        
                        if (collectionVectorSize.HasValue)
                        {
                            Console.WriteLine($"[BuildQdrantRagContext] Vector dimension validated: {queryVector.Length} matches collection config ({collectionVectorSize.Value})");
                        }
                        else
                        {
                            Console.WriteLine($"[BuildQdrantRagContext] WARNING: Could not determine collection vector size from config, proceeding with search...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BuildQdrantRagContext] WARNING: Could not validate vector dimension: {ex.Message}, proceeding with search...");
                }
                
                string searchJson = null;
                try
                {
                    searchJson = await _qdrantService.SearchAsync(collectionName, queryVector, null, searchLimit, true);
                }
                catch (System.Net.Http.HttpRequestException ex)
                {
                    Console.WriteLine($"[BuildQdrantRagContext] ERROR: Qdrant search failed with HTTP error: {ex.Message}");
                    // Try to get response body if available
                    if (ex.Data.Contains("Response"))
                    {
                        Console.WriteLine($"[BuildQdrantRagContext] Response: {ex.Data["Response"]}");
                    }
                    // Fallback to database retrieval
                    Console.WriteLine($"[BuildQdrantRagContext] Falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BuildQdrantRagContext] ERROR: Qdrant search failed: {ex.Message}");
                    Console.WriteLine($"[BuildQdrantRagContext] Stack trace: {ex.StackTrace}");
                    // Fallback to database retrieval
                    Console.WriteLine($"[BuildQdrantRagContext] Falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }
                
                if (string.IsNullOrWhiteSpace(searchJson))
                {
                    Console.WriteLine($"[BuildQdrantRagContext] WARNING: Qdrant search returned empty result, falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }

                Console.WriteLine($"[BuildQdrantRagContext] Qdrant search result length: {searchJson.Length}");
                var search = JsonSerializer.Deserialize<QdrantSearchResponse>(searchJson);
                
                if (search?.result == null || search.result.Count == 0)
                {
                    Console.WriteLine($"[BuildQdrantRagContext] WARNING: No results found in Qdrant search, falling back to database retrieval...");
                    return await PerformRAGFromKnowledgeSource(aiConfigureId, limit);
                }
                
                Console.WriteLine($"[BuildQdrantRagContext] Found {search.result.Count} results from Qdrant");

                foreach (var hit in search.result.Take(10))
                {
                    // Try to extract chunk text and (optional) knowledgeSourceId from payload
                    var text = GetPayloadValue(hit.payload, "text") ?? string.Empty;
                    var knowledgeSourceIdStr = GetPayloadValue(hit.payload, "knowledgeSourceId");

                    string sourceLabel = "Unknown";
                    if (Guid.TryParse(knowledgeSourceIdStr, out var ksId))
                    {
                        try
                        {
                            var ks = await _unitOfWork.KnowledgeSources.GetByIdAsync(ksId);
                            if (ks != null)
                            {
                                sourceLabel = ks.Source ?? ks.Title ?? sourceLabel;
                            }
                        }
                        catch { }
                    }

                    rag.RetrievedChunks.Add(new RetrievedChunkDto
                    {
                        Id = hit.id.ToString(),
                        Source = sourceLabel,
                        Content = text,
                        Score = hit.score
                    });
                }

                if (rag.RetrievedChunks.Count > 10)
                {
                    rag.RetrievedChunks = rag.RetrievedChunks
                        .OrderByDescending(c => c.Score)
                        .Take(10)
                        .ToList();
                }

                return rag;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BuildQdrantRagContext error: {ex.Message}");
                return rag;
            }
        }
    }

    // Helper classes for API responses
    public class QdrantSearchResponse
    {
        public List<QdrantSearchResult> result { get; set; }
    }

    public class QdrantSearchResult
    {
        public ulong id { get; set; }
        public float score { get; set; }
        public object payload { get; set; }
    }
}
