using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IKnowledgeSourceService
    {
        Task<ApiResponse<IEnumerable<object>>> GetBySourceAsync(string source);
        Task<ApiResponse<IEnumerable<object>>> GetByAIConfigureIdAsync(Guid aiConfigureId);
        Task<ApiResponse<bool>> DeleteBySourceAsync(string source);
        Task<ApiResponse<bool>> DeleteByAIConfigureIdAsync(Guid aiConfigureId);
    }
}


