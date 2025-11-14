using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IUserAiConfigService
    {
        Task<ApiResponse<UserAiConfigDetailResponseDto>> CreateAsync(UserAiConfigCreateDto dto);
        Task<ApiResponse<UserAiConfigDetailResponseDto>> GetByIdAsync(Guid userId, Guid aiConfigureId);
        Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetAllAsync();
        Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetByUserIdAsync(Guid userId);
        Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetByAIConfigureIdAsync(Guid aiConfigureId);
        Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid aiConfigureId);
        Task<ApiResponse<bool>> HasAccessAsync(Guid userId, Guid aiConfigureId);
    }
}
