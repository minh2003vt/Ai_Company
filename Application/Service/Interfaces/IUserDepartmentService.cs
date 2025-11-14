using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IUserDepartmentService
    {
        Task<ApiResponse<UserDepartmentResponseDto>> CreateAsync(UserDepartmentCreateDto dto);
        Task<ApiResponse<UserDepartmentResponseDto>> UpdateAsync(Guid userId, Guid departmentId, UserDepartmentUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid departmentId);
        Task<ApiResponse<bool>> DeleteByUserIdAsync(Guid userId);
        Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByDepartmentAsync(Guid departmentId);
        Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByUserAsync(Guid userId);
        Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByCompanyAsync(Guid companyId);
        Task<ApiResponse<UserDepartmentResponseDto>> AssignUserToDepartmentAsync(AssignUserToDepartmentDto dto);
    }
}
