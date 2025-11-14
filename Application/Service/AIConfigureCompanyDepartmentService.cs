using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using System;
using System.Linq;

namespace Application.Service
{
    public class AIConfigureCompanyDepartmentService : IAIConfigureCompanyDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserAiConfigService _userAiConfigService;

        public AIConfigureCompanyDepartmentService(IUnitOfWork unitOfWork, IUserAiConfigService userAiConfigService)
        {
            _unitOfWork = unitOfWork;
            _userAiConfigService = userAiConfigService;
        }

        public async Task<ApiResponse<object>> CreateAsync(Guid aiConfigureCompanyId, Guid departmentId)
        {
            try
            {
                var aiConfigureCompany = await _unitOfWork.AIConfigureCompanies.GetByIdAsync(aiConfigureCompanyId);
                if (aiConfigureCompany == null)
                {
                    return ApiResponse<object>.Fail(null, "Liên kết AI-Công ty không tồn tại");
                }

                var department = await _unitOfWork.Departments.GetByIdAsync(departmentId);
                if (department == null)
                {
                    return ApiResponse<object>.Fail(null, "Phòng ban không tồn tại");
                }

                var exists = await _unitOfWork.AIConfigureCompanyDepartments.FindAsync(
                    x => x.AIConfigureCompanyId == aiConfigureCompanyId && x.DepartmentId == departmentId);
                if (exists.Any())
                {
                    return ApiResponse<object>.Fail(null, "Liên kết đã tồn tại");
                }

                var link = new AI_ConfigureCompanyDepartment
                {
                    AIConfigureCompanyId = aiConfigureCompanyId,
                    DepartmentId = departmentId
                };
                await _unitOfWork.AIConfigureCompanyDepartments.AddAsync(link);
                await _unitOfWork.SaveChangesAsync();

                // Tự động thêm quyền sử dụng AI cho các Manager của department
                var aiConfigureId = aiConfigureCompany.AIConfigureId;
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(
                    ud => ud.DepartmentId == departmentId);
                
                var managersAdded = 0;
                foreach (var userDept in userDepartments)
                {
                    var role = await _unitOfWork.Roles.GetByIdAsync(userDept.RoleId);
                    if (role != null && string.Equals(role.Name, "Manager", StringComparison.OrdinalIgnoreCase))
                    {
                        // Kiểm tra xem đã có quyền chưa
                        var existingConfig = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(
                            userDept.UserId, aiConfigureId);
                        
                        if (existingConfig == null)
                        {
                            // Kiểm tra xem user có phải là người tạo AI không
                            var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
                            if (aiConfigure != null && aiConfigure.CreatedByUserId != userDept.UserId)
                            {
                                var userAiConfig = new UserAiConfig
                                {
                                    UserId = userDept.UserId,
                                    AIConfigureId = aiConfigureId,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await _unitOfWork.UserAiConfigs.AddAsync(userAiConfig);
                                managersAdded++;
                            }
                        }
                    }
                }

                if (managersAdded > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                return ApiResponse<object>.Ok(
                    new { 
                        link.Id, 
                        link.AIConfigureCompanyId, 
                        link.DepartmentId,
                        ManagersGrantedAccess = managersAdded
                    }, 
                    $"Tạo liên kết thành công. Đã cấp quyền cho {managersAdded} manager(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(null, $"Lỗi khi tạo liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetAllAsync()
        {
            try
            {
                var links = await _unitOfWork.AIConfigureCompanyDepartments.GetAllAsync();
                var result = new List<object>();
                foreach (var l in links)
                {
                    var aiConfigureCompany = await _unitOfWork.AIConfigureCompanies.GetByIdAsync(l.AIConfigureCompanyId);
                    var department = await _unitOfWork.Departments.GetByIdAsync(l.DepartmentId);
                    var company = aiConfigureCompany != null 
                        ? await _unitOfWork.Companies.GetByIdAsync(aiConfigureCompany.CompanyId) 
                        : null;
                    var aiConfigure = aiConfigureCompany != null 
                        ? await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureCompany.AIConfigureId) 
                        : null;
                    
                    result.Add(new 
                    { 
                        l.Id, 
                        l.AIConfigureCompanyId, 
                        l.DepartmentId,
                        DepartmentName = department?.Name,
                        CompanyName = company?.CompanyName,
                        AIName = aiConfigure?.Name
                    });
                }
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách liên kết");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi khi lấy danh sách liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetByCompanyIdAsync(Guid companyId)
        {
            try
            {
                // Lấy tất cả AIConfigureCompany của company này
                var aiConfigureCompanies = await _unitOfWork.AIConfigureCompanies.FindAsync(
                    acc => acc.CompanyId == companyId);
                var aiConfigureCompanyIds = aiConfigureCompanies.Select(acc => acc.Id).ToList();

                if (!aiConfigureCompanyIds.Any())
                {
                    return ApiResponse<IEnumerable<object>>.Ok(new List<object>(), "Không tìm thấy liên kết nào");
                }

                // Lấy tất cả AIConfigureCompanyDepartment có AIConfigureCompanyId trong danh sách
                var links = await _unitOfWork.AIConfigureCompanyDepartments.FindAsync(
                    l => aiConfigureCompanyIds.Contains(l.AIConfigureCompanyId));
                
                var result = new List<object>();
                foreach (var l in links)
                {
                    var aiConfigureCompany = await _unitOfWork.AIConfigureCompanies.GetByIdAsync(l.AIConfigureCompanyId);
                    var department = await _unitOfWork.Departments.GetByIdAsync(l.DepartmentId);
                    var company = aiConfigureCompany != null 
                        ? await _unitOfWork.Companies.GetByIdAsync(aiConfigureCompany.CompanyId) 
                        : null;
                    var aiConfigure = aiConfigureCompany != null 
                        ? await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureCompany.AIConfigureId) 
                        : null;
                    
                    result.Add(new 
                    { 
                        l.Id, 
                        l.AIConfigureCompanyId, 
                        l.DepartmentId,
                        DepartmentName = department?.Name,
                        CompanyName = company?.CompanyName,
                        AIName = aiConfigure?.Name
                    });
                }
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách liên kết theo công ty");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi khi lấy danh sách liên kết theo công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var link = await _unitOfWork.AIConfigureCompanyDepartments.GetByIdAsync(id);
                if (link == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết");
                }

                // Lấy thông tin trước khi xóa để xóa quyền của user
                var aiConfigureCompany = await _unitOfWork.AIConfigureCompanies.GetByIdAsync(link.AIConfigureCompanyId);
                var departmentId = link.DepartmentId;
                var aiConfigureId = aiConfigureCompany?.AIConfigureId ?? Guid.Empty;

                // Xóa quyền sử dụng AI của tất cả user (Admin, Manager, Staff) trong department với cascade
                var totalUsersRemoved = 0;
                if (aiConfigureCompany != null)
                {
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(
                        ud => ud.DepartmentId == departmentId);
                    
                    foreach (var userDept in userDepartments)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(userDept.RoleId);
                        if (role != null)
                        {
                            var roleName = role.Name.ToLower();
                            // Xóa quyền của Admin, Manager, Staff (không xóa SystemAdmin)
                            if (roleName == "admin" || roleName == "manager" || roleName == "staff")
                            {
                                // Kiểm tra xem có quyền không
                                var userAiConfig = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(
                                    userDept.UserId, aiConfigureId);
                                
                                if (userAiConfig != null)
                                {
                                    // Gọi DeleteAsync để có cascade delete (xóa user này + tất cả cấp dưới)
                                    var deleteResult = await _userAiConfigService.DeleteAsync(userDept.UserId, aiConfigureId);
                                    if (deleteResult.Success)
                                    {
                                        totalUsersRemoved++;
                                    }
                                }
                            }
                        }
                    }
                }

                // Xóa link
                _unitOfWork.AIConfigureCompanyDepartments.Delete(link);
                
                // Lưu tất cả changes (xóa quyền + xóa link)
                await _unitOfWork.SaveChangesAsync();
                
                var message = totalUsersRemoved > 0 
                    ? $"Xóa thành công. Đã xóa quyền của {totalUsersRemoved} user(s) và tất cả user cấp dưới"
                    : "Xóa thành công";
                
                return ApiResponse<bool>.Ok(true, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }
    }
}

