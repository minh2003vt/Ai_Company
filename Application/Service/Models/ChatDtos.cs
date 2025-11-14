using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class ChatRequestDto
    {
        [Required(ErrorMessage = "Chat Session ID là bắt buộc")]
        public Guid ChatSessionId { get; set; }

        [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc")]
            public string Text { get; set; }
    }

    public class ChatResponseDto
    {
        public string Response { get; set; }
        public List<RetrievedChunkDto> RetrievedChunks { get; set; } = new List<RetrievedChunkDto>();
        public string SessionId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RetrievedChunkDto
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Content { get; set; }
        public float Score { get; set; }
    }

    public class ConversationContext
    {
        public string UserRequest { get; set; }
        public RagContext Rag { get; set; }
        public string GeminiResponse { get; set; }
    }

    public class RagContext
    {
        public List<RetrievedChunkDto> RetrievedChunks { get; set; } = new List<RetrievedChunkDto>();
        public List<string> Rules { get; set; } = new List<string>();
    }

    public class SearchRequestDto
    {
        public string Query { get; set; }
    }
}
