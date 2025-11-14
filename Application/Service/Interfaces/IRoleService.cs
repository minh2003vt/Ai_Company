using Application.Service.Models;
using Domain.Entitites;

namespace Application.Service.Interfaces
{
    public interface IRoleService
    {
        Task<ApiResponse<RoleResponseDto>> CreateAsync(RoleCreateDto dto);
        Task<ApiResponse<RoleResponseDto>> GetByIdAsync(Guid id);
        Task<ApiResponse<IEnumerable<RoleResponseDto>>> GetAllAsync();
        Task<ApiResponse<RoleResponseDto>> UpdateAsync(Guid id, RoleUpdateDto dto);
        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
