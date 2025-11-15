using Application.Service.Models;
using Domain.Entitites;

namespace Application.Service.Interfaces
{
    public interface IAIModelConfigService
    {
        Task<ApiResponse<AIModelConfigResponseDto>> CreateAsync(AIModelConfigDto dto);
        Task<ApiResponse<AIModelConfigResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<ModelConfigApiKeyResponseDto>> GetApiKeyAsync(Guid id, string password);
        Task<ApiResponse<AIModelConfigResponseDto>> UpdateAsync(Guid id, AIModelConfigDto dto);
        Task<ApiResponse<bool>> UpdateApiKeyAsync(Guid id, string password, string newApiKey);
        Task<ApiResponse<IEnumerable<AIModelConfigResponseDto>>> GetAllAsync();
        Task<ApiResponse<bool>> UpdatePasswordAsync(Guid id, string oldPassword, string newPassword);
        Task<ApiResponse<bool>> SetActiveModelAsync(Guid id);
    }
}

