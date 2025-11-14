using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Domain.Entitites.Enums;
using Infrastructure.Repository.Interfaces;
using System;

namespace Application.Service
{
    public class AIConfigureCompanyService : IAIConfigureCompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserAiConfigService _userAiConfigService;

        public AIConfigureCompanyService(IUnitOfWork unitOfWork, IUserAiConfigService userAiConfigService)
        {
            _unitOfWork = unitOfWork;
            _userAiConfigService = userAiConfigService;
        }

        public async Task<ApiResponse<object>> CreateAsync(Guid companyId, Guid aiConfigureId)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
                if (company == null)
                {
                    return ApiResponse<object>.Fail(null, "Công ty không tồn tại");
                }

                var ai = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);
                if (ai == null)
                {
                    return ApiResponse<object>.Fail(null, "AI cấu hình không tồn tại");
                }

                if (ai.Kind != AI_ConfigureKind.Global || ai.CompanyId.HasValue)
                {
                    return ApiResponse<object>.Fail(null, "Chỉ liên kết AI Global (CompanyId = null)");
                }

                var exists = await _unitOfWork.AIConfigureCompanies.FindAsync(x => x.CompanyId == companyId && x.AIConfigureId == aiConfigureId);
                if (exists.Any())
                {
                    return ApiResponse<object>.Fail(null, "Liên kết đã tồn tại");
                }

                var link = new AI_ConfigureCompany
                {
                    CompanyId = companyId,
                    AIConfigureId = aiConfigureId
                };
                await _unitOfWork.AIConfigureCompanies.AddAsync(link);
                await _unitOfWork.SaveChangesAsync();

                // Tự động thêm quyền sử dụng AI cho các Admin của company
                var usersInCompany = await _unitOfWork.Users.FindAsync(u => u.CompanyId == companyId);
                var adminsAdded = 0;
                
                foreach (var user in usersInCompany)
                {
                    // Lấy roles của user
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == user.Id);
                    bool isAdmin = false;
                    
                    foreach (var userDept in userDepartments)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(userDept.RoleId);
                        if (role != null && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            isAdmin = true;
                            break;
                        }
                    }
                    
                    if (isAdmin)
                    {
                        // Kiểm tra xem đã có quyền chưa
                        var existingConfig = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(
                            user.Id, aiConfigureId);
                        
                        if (existingConfig == null)
                        {
                            // Kiểm tra xem user có phải là người tạo AI không
                            if (ai.CreatedByUserId != user.Id)
                            {
                                var userAiConfig = new UserAiConfig
                                {
                                    UserId = user.Id,
                                    AIConfigureId = aiConfigureId,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await _unitOfWork.UserAiConfigs.AddAsync(userAiConfig);
                                adminsAdded++;
                            }
                        }
                    }
                }

                if (adminsAdded > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                var message = adminsAdded > 0
                    ? $"Tạo liên kết thành công. Đã cấp quyền cho {adminsAdded} admin(s)"
                    : "Tạo liên kết thành công";

                return ApiResponse<object>.Ok(
                    new { 
                        link.Id, 
                        link.CompanyId, 
                        link.AIConfigureId,
                        AdminsGrantedAccess = adminsAdded
                    }, 
                    message);
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
                var links = await _unitOfWork.AIConfigureCompanies.GetAllAsync();
                var result = new List<object>();
                foreach (var l in links)
                {
                    var c = await _unitOfWork.Companies.GetByIdAsync(l.CompanyId);
                    var a = await _unitOfWork.AIConfigures.GetByIdAsync(l.AIConfigureId);
                    result.Add(new { l.Id, l.CompanyId, CompanyName = c?.CompanyName, l.AIConfigureId, AIName = a?.Name });
                }
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách liên kết");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi khi lấy danh sách liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var link = await _unitOfWork.AIConfigureCompanies.GetByIdAsync(id);
                if (link == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết");
                }

                // Lấy thông tin trước khi xóa để xóa quyền của user
                var aiConfigureId = link.AIConfigureId;
                
                // Tìm tất cả AI_ConfigureCompanyDepartment liên quan
                var departmentLinks = await _unitOfWork.AIConfigureCompanyDepartments.FindAsync(
                    accd => accd.AIConfigureCompanyId == id);
                
                var totalUsersRemoved = 0;
                
                // Xóa quyền của tất cả user (Admin, Manager, Staff) trong từng department với cascade
                foreach (var deptLink in departmentLinks)
                {
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(
                        ud => ud.DepartmentId == deptLink.DepartmentId);
                    
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

                // Xóa link (cascade sẽ tự động xóa department links)
                _unitOfWork.AIConfigureCompanies.Delete(link);
                
                // Lưu tất cả changes (xóa quyền + xóa link)
                await _unitOfWork.SaveChangesAsync();
                
                var message = totalUsersRemoved > 0 
                    ? $"Xóa thành công. Đã xóa quyền của {totalUsersRemoved} user(s) và tất cả user cấp dưới từ {departmentLinks.Count()} department(s)"
                    : "Xóa thành công";
                
                return ApiResponse<bool>.Ok(true, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteByKeysAsync(Guid companyId, Guid aiConfigureId)
        {
            try
            {
                var links = await _unitOfWork.AIConfigureCompanies
                    .FindAsync(x => x.CompanyId == companyId && x.AIConfigureId == aiConfigureId);
                var link = links.FirstOrDefault();
                if (link == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết theo CompanyId và AIConfigureId");
                }

                // Lấy thông tin trước khi xóa để xóa quyền của user
                // Tìm tất cả AI_ConfigureCompanyDepartment liên quan
                var departmentLinks = await _unitOfWork.AIConfigureCompanyDepartments.FindAsync(
                    accd => accd.AIConfigureCompanyId == link.Id);
                
                var totalUsersRemoved = 0;
                
                // Xóa quyền của tất cả user (Admin, Manager, Staff) trong từng department với cascade
                foreach (var deptLink in departmentLinks)
                {
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(
                        ud => ud.DepartmentId == deptLink.DepartmentId);
                    
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

                // Xóa link (cascade sẽ tự động xóa department links)
                _unitOfWork.AIConfigureCompanies.Delete(link);
                
                // Lưu tất cả changes (xóa quyền + xóa link)
                await _unitOfWork.SaveChangesAsync();
                
                var message = totalUsersRemoved > 0 
                    ? $"Xóa thành công. Đã xóa quyền của {totalUsersRemoved} user(s) và tất cả user cấp dưới từ {departmentLinks.Count()} department(s)"
                    : "Xóa thành công";
                
                return ApiResponse<bool>.Ok(true, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetCompaniesByAIConfigureIdAsync(Guid aiConfigureId)
        {
            try
            {
                var links = await _unitOfWork.AIConfigureCompanies.FindAsync(l => l.AIConfigureId == aiConfigureId);
                var result = new List<object>();
                foreach (var l in links)
                {
                    var c = await _unitOfWork.Companies.GetByIdAsync(l.CompanyId);
                    result.Add(new { CompanyId = l.CompanyId, CompanyName = c?.CompanyName });
                }
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách công ty theo AI");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<object>>> GetAIConfiguresByCompanyIdAsync(Guid companyId)
        {
            try
            {
                var links = await _unitOfWork.AIConfigureCompanies.FindAsync(l => l.CompanyId == companyId);
                var result = new List<object>();
                foreach (var l in links)
                {
                    var a = await _unitOfWork.AIConfigures.GetByIdAsync(l.AIConfigureId);
                    result.Add(new 
                    { 
                        Id = l.Id,
                        AIConfigureId = l.AIConfigureId, 
                        AIName = a?.Name, 
                        CurrentVersion = a?.CurrentVersion 
                    });
                }
                return ApiResponse<IEnumerable<object>>.Ok(result, "Danh sách AI theo công ty");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<object>>.Fail(null, $"Lỗi: {ex.Message}");
            }
        }
    }
}


