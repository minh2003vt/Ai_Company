using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IChatService
    {
        Task<ApiResponse<ChatResponseDto>> ProcessChatAsync(ChatRequestDto request, Guid userId);
        Task<ApiResponse<List<ChatSessionDto>>> GetUserChatSessionsAsync(Guid userId);
        Task<ApiResponse<List<ChatSessionDto>>> GetUserChatSessionsByAIAsync(Guid userId, Guid aiConfigureId);
        Task<ApiResponse<List<ChatSessionDto>>> GetChatSessionsByUserIdAsync(Guid targetUserId);
        Task<ApiResponse<ChatSessionDto>> CreateChatSessionAsync(Guid aiConfigureId, Guid userId, string title = null);
        Task<ApiResponse<bool>> DeleteChatSessionAsync(Guid sessionId, Guid userId);
    }

    public class ChatSessionDto
    {
        public Guid Id { get; set; }
        public Guid AIConfigureId { get; set; }
        public string AIConfigureName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ExternalSessionId { get; set; } = string.Empty;
    }
}
