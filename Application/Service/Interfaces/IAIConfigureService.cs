using Application.Service.Models;
using Domain.Entitites;
using Domain.Entitites.Enums;

namespace Application.Service.Interfaces
{
    public interface IAIConfigureService
    {
        Task<ApiResponse<AIConfigureDetailResponseDto>> CreateAsync(AIConfigureDto dto, Guid userId);
        Task<ApiResponse<AIConfigureDetailResponseDto>> GetByIdAsync(Guid id, Guid? userId = null);
        Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllAsync(Guid? userId = null);
        Task<ApiResponse<AIConfigureDetailResponseDto>> UpdateAsync(Guid id, AIConfigureUpdateDto dto, Guid userId);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
        Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllForUserAsync(Guid userId);
        Task<ApiResponse<int>> RecalculateRagTopKAsync(Guid aiConfigureId);
        Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllByKindAsync(AI_ConfigureKind kind, Guid? userId = null);

        // Versioning
        Task<ApiResponse<IEnumerable<(string version, DateTime createdAt)>>> GetVersionsAsync(Guid aiConfigureId);
        Task<ApiResponse<AIConfigureDetailResponseDto>> GetVersionAsync(Guid aiConfigureId, string version);
        Task<ApiResponse<AIConfigureDetailResponseDto>> RollbackAsync(Guid aiConfigureId, string version, Guid userId);
    }
}
