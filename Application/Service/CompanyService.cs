using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Service
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<CompanyResponseDto>> CreateAsync(CompanyCreateDto dto)
        {
            try
            {
                // Kiểm tra tên công ty đã tồn tại chưa
                var existingCompanyName = await _unitOfWork.Companies.FindAsync(c => c.CompanyName == dto.CompanyName);
                if (existingCompanyName.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Tên công ty đã tồn tại");
                }

                // Tạo mã công ty tự động với số thứ tự
                var generatedCompanyCode = await GenerateCompanyCodeAsync(dto.CompanyCode);

                var company = new Company
                {
                    CompanyCode = generatedCompanyCode,
                    CompanyName = dto.CompanyName,
                    Description = dto.Description,
                    Website = dto.Website,
                    MaximumUser = dto.MaximumUser,
                    SubscriptionPlan = dto.SubscriptionPlan,
                    StartSubscriptionDate = DateTime.UtcNow,
                    Status = dto.Status
                };

                await _unitOfWork.Companies.AddAsync(company);
                await _unitOfWork.SaveChangesAsync();

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    Website = company.Website,
                    MaximumUser = company.MaximumUser,
                    SubscriptionPlan = company.SubscriptionPlan,
                    StartSubscriptionDate = company.StartSubscriptionDate,
                    Status = company.Status,
                    DepartmentCount = 0,
                    AdminCount = 0,
                    ManagerCount = 0,
                    StaffCount = 0
                };

                return ApiResponse<CompanyResponseDto>.Ok(response, "Tạo công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<CompanyResponseDto>.Fail(null, $"Lỗi khi tạo công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompanyResponseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(id);
                if (company == null)
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Không tìm thấy công ty");
                }

                // Đếm số phòng ban và người dùng theo role
                var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == id);
                var roleCounts = await GetRoleCountsByCompanyAsync(id);

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    Website = company.Website,
                    MaximumUser = company.MaximumUser,
                    SubscriptionPlan = company.SubscriptionPlan,
                    StartSubscriptionDate = company.StartSubscriptionDate,
                    Status = company.Status,
                    DepartmentCount = departmentCount,
                    AdminCount = roleCounts.AdminCount,
                    ManagerCount = roleCounts.ManagerCount,
                    StaffCount = roleCounts.StaffCount
                };

                return ApiResponse<CompanyResponseDto>.Ok(response, "Lấy thông tin công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<CompanyResponseDto>.Fail(null, $"Lỗi khi lấy thông tin công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<CompanyBasicDto>>> GetAllAsync()
        {
            try
            {
                var companies = await _unitOfWork.Companies.GetAllAsync();
                var response = new List<CompanyBasicDto>();

                foreach (var company in companies)
                {
                    var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == company.Id);
                    var roleCounts = await GetRoleCountsByCompanyAsync(company.Id);

                    response.Add(new CompanyBasicDto
                    {
                        Id = company.Id,
                        CompanyCode = company.CompanyCode,
                        CompanyName = company.CompanyName,
                        Website = company.Website,
                        Status = company.Status,
                        DepartmentCount = departmentCount,
                        AdminCount = roleCounts.AdminCount,
                        ManagerCount = roleCounts.ManagerCount,
                        StaffCount = roleCounts.StaffCount
                    });
                }

                return ApiResponse<IEnumerable<CompanyBasicDto>>.Ok(response, "Lấy danh sách công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<CompanyBasicDto>>.Fail(null, $"Lỗi khi lấy danh sách công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompanyResponseDto>> UpdateAsync(Guid id, CompanyUpdateDto dto)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(id);
                if (company == null)
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Không tìm thấy công ty");
                }


                // Kiểm tra tên công ty đã tồn tại chưa (trừ chính nó)
                var existingCompanyName = await _unitOfWork.Companies.FindAsync(c => c.CompanyName == dto.CompanyName && c.Id != id);
                if (existingCompanyName.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Tên công ty đã tồn tại");
                }

                company.CompanyName = dto.CompanyName;
                company.Description = dto.Description;
                company.Website = dto.Website;
                company.MaximumUser = dto.MaximumUser;
                company.SubscriptionPlan = dto.SubscriptionPlan;

                _unitOfWork.Companies.Update(company);
                await _unitOfWork.SaveChangesAsync();

                var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == company.Id);
                var roleCounts = await GetRoleCountsByCompanyAsync(company.Id);

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    Website = company.Website,
                    MaximumUser = company.MaximumUser,
                    SubscriptionPlan = company.SubscriptionPlan,
                    StartSubscriptionDate = company.StartSubscriptionDate,
                    Status = company.Status,
                    DepartmentCount = departmentCount,
                    AdminCount = roleCounts.AdminCount,
                    ManagerCount = roleCounts.ManagerCount,
                    StaffCount = roleCounts.StaffCount
                };

                return ApiResponse<CompanyResponseDto>.Ok(response, "Cập nhật công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<CompanyResponseDto>.Fail(null, $"Lỗi khi cập nhật công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(id);
                if (company == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy công ty");
                }

                // Kiểm tra xem có user nào thuộc công ty này không
                var users = await _unitOfWork.Users.FindAsync(u => u.CompanyId == id);
                if (users.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa công ty có người dùng");
                }

                // Xóa tất cả phòng ban thuộc công ty này
                var departments = await _unitOfWork.Departments.FindAsync(d => d.CompanyId == id);
                foreach (var department in departments)
                {
                    _unitOfWork.Departments.Delete(department);
                }

                _unitOfWork.Companies.Delete(company);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa công ty: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy số lượng công ty hiện có
        /// </summary>
        public async Task<ApiResponse<int>> GetCompanyCountAsync()
        {
            try
            {
                var count = await _unitOfWork.Companies.CountAsync(c => true);
                return ApiResponse<int>.Ok(count, "Lấy số lượng công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Fail(0, $"Lỗi khi lấy số lượng công ty: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy số lượng người dùng theo role trong một công ty
        /// </summary>
        private async Task<(int AdminCount, int ManagerCount, int StaffCount)> GetRoleCountsByCompanyAsync(Guid companyId)
        {
            try
            {
                // Lấy tất cả users trong company
                var users = await _unitOfWork.Users.FindAsync(u => u.CompanyId == companyId);
                
                int adminCount = 0;
                int managerCount = 0;
                int staffCount = 0;

                foreach (var user in users)
                {
                    // Lấy roles từ UserDepartment
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == user.Id);
                    foreach (var ud in userDepartments)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                        if (role != null)
                        {
                            switch (role.Name.ToLower())
                            {
                                case "admin":
                                    adminCount++;
                                    break;
                                case "manager":
                                    managerCount++;
                                    break;
                                case "staff":
                                    staffCount++;
                                    break;
                            }
                        }
                    }
                }

                return (adminCount, managerCount, staffCount);
            }
            catch (Exception)
            {
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Tạo mã công ty tự động với số thứ tự
        /// Ví dụ: "oneads" -> "oneads0002" (nếu đã có 1 company trong DB)
        /// </summary>
        private async Task<string> GenerateCompanyCodeAsync(string baseCode)
        {
            // Đếm số lượng công ty hiện có
            var companyCount = await _unitOfWork.Companies.CountAsync(c => true);
            
            // Tạo mã công ty với số thứ tự (bắt đầu từ 0002)
            var nextNumber = companyCount + 2; // +2 vì bắt đầu từ 0002
            var companyCode = $"{baseCode}{nextNumber:D4}"; // D4 để có 4 chữ số với leading zeros
            
            return companyCode;
        }

        public async Task<ApiResponse<CompanyResponseDto>> UpdateStatusAsync(Guid id, CompanyStatusUpdateDto dto)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(id);
                if (company == null)
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Không tìm thấy công ty");
                }

                company.Status = dto.Status;

                _unitOfWork.Companies.Update(company);
                await _unitOfWork.SaveChangesAsync();

                var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == company.Id);
                var roleCounts = await GetRoleCountsByCompanyAsync(company.Id);

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyCode = company.CompanyCode,
                    CompanyName = company.CompanyName,
                    Description = company.Description,
                    Website = company.Website,
                    MaximumUser = company.MaximumUser,
                    SubscriptionPlan = company.SubscriptionPlan,
                    StartSubscriptionDate = company.StartSubscriptionDate,
                    Status = company.Status,
                    DepartmentCount = departmentCount,
                    AdminCount = roleCounts.AdminCount,
                    ManagerCount = roleCounts.ManagerCount,
                    StaffCount = roleCounts.StaffCount
                };

                return ApiResponse<CompanyResponseDto>.Ok(response, "Cập nhật trạng thái công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<CompanyResponseDto>.Fail(null, $"Lỗi khi cập nhật trạng thái công ty: {ex.Message}");
            }
        }
    }
}
