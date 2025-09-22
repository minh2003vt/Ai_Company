using Application.Service.Models;
using Domain.Entitites;

namespace Application.Service.Interfaces
{
    public interface IAIConfigureService
    {
        Task<ApiResponse<AIConfigureResponseDto>> CreateAsync(AIConfigureCreateDto dto, Guid userId);
        Task<ApiResponse<AIConfigureResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<AIConfigureResponseDto>>> GetAllAsync();
        Task<ApiResponse<AIConfigureResponseDto>> UpdateAsync(Guid id, AIConfigureUpdateDto dto, Guid userId);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
