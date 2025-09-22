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
                // Kiểm tra TIN đã tồn tại chưa
                var existingCompany = await _unitOfWork.Companies.FindAsync(c => c.TIN == dto.TIN);
                if (existingCompany.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Mã số thuế đã tồn tại");
                }

                // Kiểm tra tên công ty đã tồn tại chưa
                var existingCompanyName = await _unitOfWork.Companies.FindAsync(c => c.CompanyName == dto.CompanyName);
                if (existingCompanyName.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Tên công ty đã tồn tại");
                }

                var company = new Company
                {
                    CompanyName = dto.CompanyName,
                    TIN = dto.TIN,
                    Description = dto.Description
                };

                await _unitOfWork.Companies.AddAsync(company);
                await _unitOfWork.SaveChangesAsync();

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    TIN = company.TIN,
                    Description = company.Description,
                    DepartmentCount = 0,
                    UserCount = 0
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

                // Đếm số phòng ban và người dùng
                var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == id);
                var userCount = await _unitOfWork.UserCompanies.CountAsync(uc => uc.CompanyId == id);

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    TIN = company.TIN,
                    Description = company.Description,
                    DepartmentCount = departmentCount,
                    UserCount = userCount
                };

                return ApiResponse<CompanyResponseDto>.Ok(response, "Lấy thông tin công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<CompanyResponseDto>.Fail(null, $"Lỗi khi lấy thông tin công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<CompanyResponseDto>>> GetAllAsync()
        {
            try
            {
                var companies = await _unitOfWork.Companies.GetAllAsync();
                var response = new List<CompanyResponseDto>();

                foreach (var company in companies)
                {
                    var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == company.Id);
                    var userCount = await _unitOfWork.UserCompanies.CountAsync(uc => uc.CompanyId == company.Id);

                    response.Add(new CompanyResponseDto
                    {
                        Id = company.Id,
                        CompanyName = company.CompanyName,
                        TIN = company.TIN,
                        Description = company.Description,
                        DepartmentCount = departmentCount,
                        UserCount = userCount
                    });
                }

                return ApiResponse<IEnumerable<CompanyResponseDto>>.Ok(response, "Lấy danh sách công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<CompanyResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách công ty: {ex.Message}");
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

                // Kiểm tra TIN đã tồn tại chưa (trừ chính nó)
                var existingTIN = await _unitOfWork.Companies.FindAsync(c => c.TIN == dto.TIN && c.Id != id);
                if (existingTIN.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Mã số thuế đã tồn tại");
                }

                // Kiểm tra tên công ty đã tồn tại chưa (trừ chính nó)
                var existingCompanyName = await _unitOfWork.Companies.FindAsync(c => c.CompanyName == dto.CompanyName && c.Id != id);
                if (existingCompanyName.Any())
                {
                    return ApiResponse<CompanyResponseDto>.Fail(null, "Tên công ty đã tồn tại");
                }

                company.CompanyName = dto.CompanyName;
                company.TIN = dto.TIN;
                company.Description = dto.Description;

                _unitOfWork.Companies.Update(company);
                await _unitOfWork.SaveChangesAsync();

                var departmentCount = await _unitOfWork.Departments.CountAsync(d => d.CompanyId == company.Id);
                var userCount = await _unitOfWork.UserCompanies.CountAsync(uc => uc.CompanyId == company.Id);

                var response = new CompanyResponseDto
                {
                    Id = company.Id,
                    CompanyName = company.CompanyName,
                    TIN = company.TIN,
                    Description = company.Description,
                    DepartmentCount = departmentCount,
                    UserCount = userCount
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

                // Kiểm tra xem có phòng ban nào thuộc công ty này không
                var departments = await _unitOfWork.Departments.FindAsync(d => d.CompanyId == id);
                if (departments.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa công ty có phòng ban");
                }

                // Kiểm tra xem có user nào thuộc công ty này không
                var userCompanies = await _unitOfWork.UserCompanies.FindAsync(uc => uc.CompanyId == id);
                if (userCompanies.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không thể xóa công ty có người dùng");
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
    }
}
