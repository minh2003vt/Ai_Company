using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IAIConfigureCompanyService
    {
        Task<ApiResponse<object>> CreateAsync(Guid companyId, Guid aiConfigureId);
        Task<ApiResponse<IEnumerable<object>>> GetAllAsync();
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
        Task<ApiResponse<bool>> DeleteByKeysAsync(Guid companyId, Guid aiConfigureId);
        Task<ApiResponse<IEnumerable<object>>> GetCompaniesByAIConfigureIdAsync(Guid aiConfigureId);
        Task<ApiResponse<IEnumerable<object>>> GetAIConfiguresByCompanyIdAsync(Guid companyId);
    }
}


