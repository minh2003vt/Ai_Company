using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Service
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<RoleResponseDto>> CreateAsync(RoleCreateDto dto)
        {
            try
            {
                // Kiểm tra tên role đã tồn tại chưa
                var existingRole = await _unitOfWork.Roles.FindAsync(r => r.Name == dto.Name);
                if (existingRole.Any())
                {
                    return ApiResponse<RoleResponseDto>.Fail(null, "Tên vai trò đã tồn tại");
                }

                var role = new Role
                {
                    Name = dto.Name,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Roles.AddAsync(role);
                await _unitOfWork.SaveChangesAsync();

                var response = new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    CreatedAt = role.CreatedAt
                };

                return ApiResponse<RoleResponseDto>.Ok(response, "Tạo vai trò thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<RoleResponseDto>.Fail(null, $"Lỗi khi tạo vai trò: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RoleResponseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return ApiResponse<RoleResponseDto>.Fail(null, "Không tìm thấy vai trò");
                }

                var response = new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    CreatedAt = role.CreatedAt
                };

                return ApiResponse<RoleResponseDto>.Ok(response, "Lấy thông tin vai trò thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<RoleResponseDto>.Fail(null, $"Lỗi khi lấy thông tin vai trò: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<RoleResponseDto>>> GetAllAsync()
        {
            try
            {
                var roles = await _unitOfWork.Roles.GetAllAsync();
                var response = roles.Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    CreatedAt = r.CreatedAt
                });

                return ApiResponse<IEnumerable<RoleResponseDto>>.Ok(response, "Lấy danh sách vai trò thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<RoleResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách vai trò: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RoleResponseDto>> UpdateAsync(Guid id, RoleUpdateDto dto)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return ApiResponse<RoleResponseDto>.Fail(null, "Không tìm thấy vai trò");
                }

                // Kiểm tra tên role đã tồn tại chưa (trừ chính nó)
                var existingRole = await _unitOfWork.Roles.FindAsync(r => r.Name == dto.Name && r.Id != id);
                if (existingRole.Any())
                {
                    return ApiResponse<RoleResponseDto>.Fail(null, "Tên vai trò đã tồn tại");
                }

                role.Name = dto.Name;
                _unitOfWork.Roles.Update(role);
                await _unitOfWork.SaveChangesAsync();

                var response = new RoleResponseDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    CreatedAt = role.CreatedAt
                };

                return ApiResponse<RoleResponseDto>.Ok(response, "Cập nhật vai trò thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<RoleResponseDto>.Fail(null, $"Lỗi khi cập nhật vai trò: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy vai trò");
                }

                // Kiểm tra xem có user nào đang sử dụng role này không
                var usersWithRole = await _unitOfWork.Users.FindAsync(u => u.RoleId == id);
                if (usersWithRole.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa vai trò đang được sử dụng bởi người dùng");
                }

                _unitOfWork.Roles.Delete(role);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa vai trò thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa vai trò: {ex.Message}");
            }
        }
    }
}
