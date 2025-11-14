using Domain.Entitites;
using Domain.Entitites.Enums;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Service
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService(FirestoreDb db)
        {
            _db = db;
        }

        // Make _db accessible for ChatService
        public FirestoreDb Db => _db;

        /// <summary>
        /// Tạo chat session trên Firebase và trả về ExternalSessionId
        /// </summary>
        public async Task<string> CreateChatSessionAsync(ChatSession session)
        {
            // Tạo document trong collection "chatSessions"
            var sessionRef = _db.Collection("chatSessions").Document();
            session.ExternalSessionId = sessionRef.Id;

            // Lưu metadata session (có thể mở rộng)
            await sessionRef.SetAsync(new
            {
                AIConfigureId = session.AIConfigureId.ToString(),
                UserId = session.UserId.ToString(),
                Title = session.Title,
                CreatedAt = session.CreatedAt
            });

            return session.ExternalSessionId;
        }

        /// <summary>
        /// Lưu message vào Firebase dưới chat session đã tồn tại
        /// </summary>
        public async Task SaveChatMessageAsync(string externalSessionId, ChatMessage message)
        {
            if (string.IsNullOrEmpty(externalSessionId))
                throw new ArgumentException("Session ID is required");

            var messagesRef = _db.Collection("chatSessions")
                                 .Document(externalSessionId)
                                 .Collection("messages");

            await messagesRef.AddAsync(new
            {
                Role = message.Role.ToString(),
                Content = message.Content,
                CreatedAt = message.CreatedAt,
                KnowledgeSourceId = message.KnowledgeSourceId.HasValue ? message.KnowledgeSourceId.Value.ToString() : null,
                MetaJson = message.MetaJson
            });
        }

        /// <summary>
        /// Lưu message theo format yêu cầu: User, userName, text, timestamp
        /// </summary>
        public async Task SaveChatMessageAsync(string externalSessionId, string userId, string userName, string text, string role)
        {
            if (string.IsNullOrEmpty(externalSessionId))
                throw new ArgumentException("Session ID is required");

            var messagesRef = _db.Collection("chatSessions")
                                 .Document(externalSessionId)
                                 .Collection("messages");

            var messageId = $"msg_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            
            await messagesRef.Document(messageId).SetAsync(new
            {
                User = role == "user" ? userId : "Gemini",
                userName = role == "user" ? userName : "Gemini",
                text = text,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        /// <summary>
        /// Lấy tất cả messages của session từ Firebase, có thể giới hạn số lượng
        /// </summary>
        public async Task<List<ChatMessage>> GetMessagesAsync(string externalSessionId, int limit = 50)
        {
            var messagesRef = _db.Collection("chatSessions")
                                 .Document(externalSessionId)
                                 .Collection("messages");

            var snapshot = await messagesRef.OrderBy("CreatedAt")
                                            .Limit(limit)
                                            .GetSnapshotAsync();

            var messages = snapshot.Documents.Select(d =>
            {
                var data = d.ToDictionary();
                var roleStr = data.TryGetValue("Role", out var r) ? r?.ToString() : null;
                Domain.Entitites.Enums.MessageRole roleVal;
                Enum.TryParse(roleStr, out roleVal);
                var content = data.TryGetValue("Content", out var c) ? c?.ToString() : string.Empty;
                DateTime createdAt;
                if (data.TryGetValue("CreatedAt", out var ca) && ca is DateTime dt)
                {
                    createdAt = dt;
                }
                else
                {
                    createdAt = DateTime.UtcNow;
                }
                Guid? ksId = null;
                if (data.TryGetValue("KnowledgeSourceId", out var ks) && ks != null)
                {
                    if (Guid.TryParse(ks.ToString(), out var g)) ksId = g;
                }
                var meta = data.TryGetValue("MetaJson", out var m) ? m?.ToString() : null;

                return new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    Role = roleVal,
                    Content = content,
                    CreatedAt = createdAt,
                    KnowledgeSourceId = ksId,
                    MetaJson = meta
                };
            }).ToList();

            return messages;
        }

        /// <summary>
        /// Lấy tất cả session của một user
        /// </summary>
        public async Task<List<ChatSession>> GetSessionsByUserAsync(Guid userId)
        {
            var snapshot = await _db.Collection("chatSessions")
                                    .WhereEqualTo("UserId", userId.ToString())
                                    .GetSnapshotAsync();

            var sessions = snapshot.Documents.Select(d =>
            {
                var session = new ChatSession
                {
                    ExternalSessionId = d.Id,
                    UserId = userId,
                    AIConfigureId = Guid.TryParse(d.GetValue<string>("AIConfigureId"), out var gid) ? gid : Guid.Empty,
                    Title = d.GetValue<string>("Title"),
                    CreatedAt = d.GetValue<DateTime>("CreatedAt")
                };
                return session;
            }).ToList();

            return sessions;
        }
    }
    public class ChatMessage
    {
        public Guid Id { get; set; }

        public Guid ChatSessionId { get; set; }
        public virtual ChatSession ChatSession { get; set; }

        public MessageRole Role { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: pointer to which knowledge source produced this content (for provenance)
        public Guid? KnowledgeSourceId { get; set; }
        public virtual KnowledgeSource KnowledgeSource { get; set; }

        // Optional: store model output metadata (tokens, score)
        public string MetaJson { get; set; }
    }

}
