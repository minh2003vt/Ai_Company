using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Service.Models;

namespace Application.Service.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserMeDto>> GetMeAsync(Guid userId);
        Task<ApiResponse<UserMeDto>> UpdateMeAsync(Guid userId, UpdateUserMeDto dto);
        Task<ApiResponse<IEnumerable<UserResponseDto>>> GetUsersByCompanyAsync(Guid companyId);
        Task<ApiResponse<IEnumerable<UserResponseDto>>> GetUsersByDepartmentAsync(Guid departmentId);
        Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto);
        Task<ApiResponse<bool>> DeleteUserAsync(Guid userId);
    }
}
