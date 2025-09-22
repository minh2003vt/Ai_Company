using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Service
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DepartmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<DepartmentResponseDto>> CreateAsync(DepartmentCreateDto dto)
        {
            try
            {
                // Kiểm tra công ty có tồn tại không
                var company = await _unitOfWork.Companies.GetByIdAsync(dto.CompanyId);
                if (company == null)
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Công ty không tồn tại");
                }

                // Kiểm tra tên phòng ban đã tồn tại trong công ty chưa
                var existingDepartment = await _unitOfWork.Departments.FindAsync(d => d.Name == dto.Name && d.CompanyId == dto.CompanyId);
                if (existingDepartment.Any())
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Tên phòng ban đã tồn tại trong công ty này");
                }

                var department = new Department
                {
                    Name = dto.Name,
                    CompanyId = dto.CompanyId
                };

                await _unitOfWork.Departments.AddAsync(department);
                await _unitOfWork.SaveChangesAsync();

                var response = new DepartmentResponseDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    CompanyId = department.CompanyId,
                    CompanyName = company.CompanyName,
                    UserCount = 0
                };

                return ApiResponse<DepartmentResponseDto>.Ok(response, "Tạo phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<DepartmentResponseDto>.Fail(null, $"Lỗi khi tạo phòng ban: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DepartmentResponseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(id);
                if (department == null)
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Không tìm thấy phòng ban");
                }

                // Lấy thông tin công ty
                var company = await _unitOfWork.Companies.GetByIdAsync(department.CompanyId);
                
                // Đếm số người dùng trong phòng ban
                var userCount = await _unitOfWork.Users.CountAsync(u => u.DepartmentId == id);

                var response = new DepartmentResponseDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    CompanyId = department.CompanyId,
                    CompanyName = company?.CompanyName ?? "Unknown",
                    UserCount = userCount
                };

                return ApiResponse<DepartmentResponseDto>.Ok(response, "Lấy thông tin phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<DepartmentResponseDto>.Fail(null, $"Lỗi khi lấy thông tin phòng ban: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<DepartmentResponseDto>>> GetAllAsync()
        {
            try
            {
                var departments = await _unitOfWork.Departments.GetAllAsync();
                var response = new List<DepartmentResponseDto>();

                foreach (var department in departments)
                {
                    var company = await _unitOfWork.Companies.GetByIdAsync(department.CompanyId);
                    var userCount = await _unitOfWork.Users.CountAsync(u => u.DepartmentId == department.Id);

                    response.Add(new DepartmentResponseDto
                    {
                        Id = department.Id,
                        Name = department.Name,
                        CompanyId = department.CompanyId,
                        CompanyName = company?.CompanyName ?? "Unknown",
                        UserCount = userCount
                    });
                }

                return ApiResponse<IEnumerable<DepartmentResponseDto>>.Ok(response, "Lấy danh sách phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<DepartmentResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách phòng ban: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<DepartmentResponseDto>>> GetByCompanyIdAsync(Guid companyId)
        {
            try
            {
                // Kiểm tra công ty có tồn tại không
                var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
                if (company == null)
                {
                    return ApiResponse<IEnumerable<DepartmentResponseDto>>.Fail(null, "Công ty không tồn tại");
                }

                var departments = await _unitOfWork.Departments.FindAsync(d => d.CompanyId == companyId);
                var response = new List<DepartmentResponseDto>();

                foreach (var department in departments)
                {
                    var userCount = await _unitOfWork.Users.CountAsync(u => u.DepartmentId == department.Id);

                    response.Add(new DepartmentResponseDto
                    {
                        Id = department.Id,
                        Name = department.Name,
                        CompanyId = department.CompanyId,
                        CompanyName = company.CompanyName,
                        UserCount = userCount
                    });
                }

                return ApiResponse<IEnumerable<DepartmentResponseDto>>.Ok(response, "Lấy danh sách phòng ban theo công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<DepartmentResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách phòng ban theo công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<DepartmentResponseDto>> UpdateAsync(Guid id, DepartmentUpdateDto dto)
        {
            try
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(id);
                if (department == null)
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Không tìm thấy phòng ban");
                }

                // Kiểm tra công ty có tồn tại không
                var company = await _unitOfWork.Companies.GetByIdAsync(dto.CompanyId);
                if (company == null)
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Công ty không tồn tại");
                }

                // Kiểm tra tên phòng ban đã tồn tại trong công ty chưa (trừ chính nó)
                var existingDepartment = await _unitOfWork.Departments.FindAsync(d => d.Name == dto.Name && d.CompanyId == dto.CompanyId && d.Id != id);
                if (existingDepartment.Any())
                {
                    return ApiResponse<DepartmentResponseDto>.Fail(null, "Tên phòng ban đã tồn tại trong công ty này");
                }

                department.Name = dto.Name;
                department.CompanyId = dto.CompanyId;

                _unitOfWork.Departments.Update(department);
                await _unitOfWork.SaveChangesAsync();

                var userCount = await _unitOfWork.Users.CountAsync(u => u.DepartmentId == department.Id);

                var response = new DepartmentResponseDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    CompanyId = department.CompanyId,
                    CompanyName = company.CompanyName,
                    UserCount = userCount
                };

                return ApiResponse<DepartmentResponseDto>.Ok(response, "Cập nhật phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<DepartmentResponseDto>.Fail(null, $"Lỗi khi cập nhật phòng ban: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(id);
                if (department == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy phòng ban");
                }

                // Kiểm tra xem có user nào thuộc phòng ban này không
                var usersInDepartment = await _unitOfWork.Users.FindAsync(u => u.DepartmentId == id);
                if (usersInDepartment.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa phòng ban có người dùng");
                }

                _unitOfWork.Departments.Delete(department);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa phòng ban: {ex.Message}");
            }
        }
    }
}


