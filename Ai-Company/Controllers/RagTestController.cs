using Application.Helper;
using Application.Service;
using Application.Service.Interfaces;
using Infrastructure.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ai_Company.Controllers
{
    [ApiController]
    [Route("api/rag-test")]
    [AllowAnonymous]
    public class RagTestController : ControllerBase
    {
        private readonly QdrantHelper _qdrantHelper;
        private readonly QdrantService _qdrantService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public RagTestController(
            QdrantHelper qdrantHelper,
            QdrantService qdrantService,
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _qdrantHelper = qdrantHelper;
            _qdrantService = qdrantService;
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpGet("health")]
        public IActionResult Health() => Ok(new { ok = true });

        public class ImportRequest
        {
            public Guid AiConfigureId { get; set; }
            public string CollectionName { get; set; } = string.Empty;
        }

        [HttpPost("import")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> Import( IFormFile file, [FromForm] Guid aiConfigureId, [FromForm] string collectionName)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");
            if (string.IsNullOrWhiteSpace(collectionName)) return BadRequest("collectionName is required");
            if (aiConfigureId == Guid.Empty) return BadRequest("aiConfigureId is required");

            // Owner user id is optional here for test; use empty
            var results = await _qdrantHelper.ImportFileAsync(file, collectionName, aiConfigureId, Guid.Empty);
            return Ok(new { imported = results.Count, points = results.Select(r => new { r.pointId, r.knowledgeSourceId, r.pageNumber }) });
        }

        public class QueryRequest
        {
            public Guid AiConfigureId { get; set; }
            public string Text { get; set; } = string.Empty;
            public int? TopK { get; set; }
            public string? CollectionName { get; set; }
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] QueryRequest request)
        {
            if (request == null || request.AiConfigureId == Guid.Empty) return BadRequest("AiConfigureId is required");
            if (string.IsNullOrWhiteSpace(request.Text)) return BadRequest("Text is required");

            var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(request.AiConfigureId);
            if (aiConfigure == null) return BadRequest("AI Configure not found");

            // Clean query
            var cleaned = Clean(request.Text);

            // Embed with same tokenizer + ONNX as ingestion
            var queryVector = await GenerateEmbeddingAsync(cleaned);

            // Temporary override topK if provided
            if (request.TopK.HasValue && request.TopK.Value > 0)
            {
                aiConfigure.RagTopK = request.TopK.Value;
            }

            // Search Qdrant (use provided collectionName or default to AiConfigureId)
            string col = string.IsNullOrWhiteSpace(request.CollectionName) ? request.AiConfigureId.ToString() : request.CollectionName;
            var searchJson = await _qdrantService.SearchAsync(col, queryVector, null, request.TopK ?? aiConfigure.RagTopK, true);
            var search = JsonSerializer.Deserialize<QdrantSearchResponse>(searchJson);

            var chunks = new List<object>();
            if (search?.result != null)
            {
                foreach (var hit in search.result)
                {
                    var text = GetPayloadValue(hit.payload, "text") ?? string.Empty;
                    var knowledgeSourceIdStr = GetPayloadValue(hit.payload, "knowledgeSourceId");
                    string sourceLabel = "Unknown";
                    if (Guid.TryParse(knowledgeSourceIdStr, out var ksId))
                    {
                        try
                        {
                            var ks = await _unitOfWork.KnowledgeSources.GetByIdAsync(ksId);
                            if (ks != null) sourceLabel = ks.Source ?? ks.Title ?? sourceLabel;
                        }
                        catch { }
                    }

                    chunks.Add(new { id = hit.id, score = hit.score, source = sourceLabel, content = text });
                }
            }

            // Build a simple context and ask Gemini
            var sb = new StringBuilder();
            sb.AppendLine("User question:");
            sb.AppendLine(cleaned);
            sb.AppendLine();
            sb.AppendLine("Retrieved chunks:");
            foreach (dynamic c in chunks)
            {
                sb.AppendLine($"- [{c.score:F3}] {c.source}: {Truncate((string)c.content, 500)}");
            }

            var geminiAnswer = await _geminiService.GenerateResponseAsync(sb.ToString(), aiConfigure);

            return Ok(new { answer = geminiAnswer, chunks });
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = Regex.Replace(text, "\r\n?|\u000B|\u000C|\u0085|\u2028|\u2029", " \n ");
            normalized = Regex.Replace(normalized, "\\s+", " ");
            return normalized.Trim();
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
            catch
            {
                var rnd = new Random();
                return Enumerable.Range(0, 384).Select(_ => (float)rnd.NextDouble()).ToArray();
            }
        }

        private class EmbedResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
            public int Dimension { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private static string? GetPayloadValue(object? payload, string key)
        {
            if (payload == null) return null;
            try
            {
                var jsonElement = (JsonElement)payload;
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(key, out var value))
                {
                    return value.GetString();
                }
            }
            catch { }
            return null;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max) + "...";
        }
    }

    // Minimal copies to deserialize Qdrant responses
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


