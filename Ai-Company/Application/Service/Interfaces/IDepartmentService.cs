using Application.Service.Models;
using Domain.Entitites;

namespace Application.Service.Interfaces
{
    public interface IDepartmentService
    {
        Task<ApiResponse<DepartmentResponseDto>> CreateAsync(DepartmentCreateDto dto);
        Task<ApiResponse<DepartmentResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<DepartmentResponseDto>>> GetAllAsync();
        Task<ApiResponse<IEnumerable<DepartmentResponseDto>>> GetByCompanyIdAsync(Guid companyId);
        Task<ApiResponse<DepartmentResponseDto>> UpdateAsync(Guid id, DepartmentUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
