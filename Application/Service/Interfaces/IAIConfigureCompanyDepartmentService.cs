using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IAIConfigureCompanyDepartmentService
    {
        Task<ApiResponse<object>> CreateAsync(Guid aiConfigureCompanyId, Guid departmentId);
        Task<ApiResponse<IEnumerable<object>>> GetAllAsync();
        Task<ApiResponse<IEnumerable<object>>> GetByCompanyIdAsync(Guid companyId);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}

