using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using System;
using System.Linq;

namespace Application.Service
{
    public class UserAiConfigService : IUserAiConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserAiConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<UserAiConfigDetailResponseDto>> CreateAsync(UserAiConfigCreateDto dto)
        {
            try
            {
                // Kiểm tra user tồn tại
                var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
                if (user == null)
                {
                    return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, "Người dùng không tồn tại");
                }

                // Kiểm tra AI Configure tồn tại
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(dto.AIConfigureId);
                if (aiConfigure == null)
                {
                    return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, "Cấu hình AI không tồn tại");
                }
                if(aiConfigure.CreatedByUserId == dto.UserId)
                {
                    return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, "User này là người tạo cấu hình AI");
                }
                // Kiểm tra liên kết đã tồn tại chưa
                var existingLink = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(dto.UserId, dto.AIConfigureId);
                if (existingLink != null)
                {
                    return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, "Liên kết người dùng và AI đã tồn tại");
                }

                var userAiConfig = new UserAiConfig
                {
                    UserId = dto.UserId,
                    AIConfigureId = dto.AIConfigureId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserAiConfigs.AddAsync(userAiConfig);
                await _unitOfWork.SaveChangesAsync();

                var response = new UserAiConfigDetailResponseDto
                {
                    UserId = userAiConfig.UserId,
                    UserName = user.FullName,
                    UserEmail = user.Email,
                    AIConfigureId = userAiConfig.AIConfigureId,
                    AIConfigureName = aiConfigure.Name,
                    AIConfigureDescription = aiConfigure.Description,
                    CreatedAt = userAiConfig.CreatedAt
                };

                return ApiResponse<UserAiConfigDetailResponseDto>.Ok(response, "Tạo liên kết người dùng và AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, $"Lỗi khi tạo liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserAiConfigDetailResponseDto>> GetByIdAsync(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var userAiConfig = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(userId, aiConfigureId);
                if (userAiConfig == null)
                {
                    return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, "Không tìm thấy liên kết");
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(aiConfigureId);

                var response = new UserAiConfigDetailResponseDto
                {
                    UserId = userAiConfig.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    UserEmail = user?.Email ?? "Unknown",
                    AIConfigureId = userAiConfig.AIConfigureId,
                    AIConfigureName = aiConfigure?.Name ?? "Unknown",
                    AIConfigureDescription = aiConfigure?.Description ?? "Unknown",
                    CreatedAt = userAiConfig.CreatedAt
                };

                return ApiResponse<UserAiConfigDetailResponseDto>.Ok(response, "Lấy thông tin liên kết thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserAiConfigDetailResponseDto>.Fail(null, $"Lỗi khi lấy thông tin liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetAllAsync()
        {
            try
            {
                var userAiConfigs = await _unitOfWork.UserAiConfigs.GetAllAsync();
                var response = new List<UserAiConfigResponseDto>();

                foreach (var config in userAiConfigs)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(config.AIConfigureId);

                    response.Add(new UserAiConfigResponseDto
                    {
                        UserId = config.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        AIConfigureId = config.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        AIConfigureDescription = aiConfigure?.Description ?? "Unknown",
                        CreatedAt = config.CreatedAt
                    });
                }

                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Ok(response, "Lấy danh sách liên kết thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var userAiConfigs = await _unitOfWork.UserAiConfigs.GetByUserIdAsync(userId);
                var response = new List<UserAiConfigResponseDto>();

                foreach (var config in userAiConfigs)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(config.AIConfigureId);

                    response.Add(new UserAiConfigResponseDto
                    {
                        UserId = config.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        AIConfigureId = config.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        AIConfigureDescription = aiConfigure?.Description ?? "Unknown",
                        CreatedAt = config.CreatedAt
                    });
                }

                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Ok(response, "Lấy danh sách AI của người dùng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách AI của người dùng: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserAiConfigResponseDto>>> GetByAIConfigureIdAsync(Guid aiConfigureId)
        {
            try
            {
                var userAiConfigs = await _unitOfWork.UserAiConfigs.GetByAIConfigureIdAsync(aiConfigureId);
                var response = new List<UserAiConfigResponseDto>();

                foreach (var config in userAiConfigs)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(config.UserId);
                    var aiConfigure = await _unitOfWork.AIConfigures.GetByIdAsync(config.AIConfigureId);

                    response.Add(new UserAiConfigResponseDto
                    {
                        UserId = config.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        AIConfigureId = config.AIConfigureId,
                        AIConfigureName = aiConfigure?.Name ?? "Unknown",
                        AIConfigureDescription = aiConfigure?.Description ?? "Unknown",
                        CreatedAt = config.CreatedAt
                    });
                }

                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Ok(response, "Lấy danh sách người dùng của AI thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserAiConfigResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách người dùng của AI: {ex.Message}");
            }
        }


        /// <summary>
        /// Lấy role level để so sánh hierarchy: SystemAdmin (4) > Admin (3) > Manager (2) > Staff (1)
        /// </summary>
        private int GetRoleLevel(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return 0;
            
            var role = roleName.ToLower();
            return role switch
            {
                "systemadmin" => 4,
                "admin" => 3,
                "manager" => 2,
                "staff" => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Xóa cascade quyền của tất cả user cấp dưới khi xóa quyền của một user
        /// Hierarchy: SystemAdmin > Admin > Manager > Staff
        /// </summary>
        public async Task<int> DeleteCascadeAccessAsync(Guid userId, Guid aiConfigureId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return 0;

            // Lấy role cao nhất của user
            var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
            int maxRoleLevel = 0;
            Guid? userCompanyId = user.CompanyId;
            var userDepartmentIds = userDepartments.Select(ud => ud.DepartmentId).Where(id => id.HasValue).Select(id => id.Value).ToList();

            foreach (var ud in userDepartments)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                if (role != null)
                {
                    var level = GetRoleLevel(role.Name);
                    if (level > maxRoleLevel)
                        maxRoleLevel = level;
                }
            }

            if (maxRoleLevel == 0) return 0; // Không có role hợp lệ

            // Tìm tất cả user có quyền với AI này
            var allUserAiConfigs = await _unitOfWork.UserAiConfigs.GetByAIConfigureIdAsync(aiConfigureId);
            var usersToRemove = new List<UserAiConfig>();

            foreach (var config in allUserAiConfigs)
            {
                if (config.UserId == userId) continue; // Bỏ qua chính user đang xóa

                var targetUser = await _unitOfWork.Users.GetByIdAsync(config.UserId);
                if (targetUser == null) continue;

                // Lấy role cao nhất của target user
                var targetUserDepts = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == config.UserId);
                int targetMaxRoleLevel = 0;
                bool isInSameScope = false;

                foreach (var tud in targetUserDepts)
                {
                    var role = await _unitOfWork.Roles.GetByIdAsync(tud.RoleId);
                    if (role != null)
                    {
                        var level = GetRoleLevel(role.Name);
                        if (level > targetMaxRoleLevel)
                            targetMaxRoleLevel = level;

                        // Kiểm tra cùng scope: cùng company hoặc cùng department
                        if (maxRoleLevel >= 3) // SystemAdmin hoặc Admin: xóa tất cả trong company
                        {
                            if (targetUser.CompanyId == userCompanyId)
                                isInSameScope = true;
                        }
                        else if (maxRoleLevel == 2) // Manager: xóa trong cùng department
                        {
                            if (tud.DepartmentId.HasValue && userDepartmentIds.Contains(tud.DepartmentId.Value))
                                isInSameScope = true;
                        }
                    }
                }

                // Xóa nếu: role thấp hơn VÀ cùng scope
                if (targetMaxRoleLevel < maxRoleLevel && isInSameScope)
                {
                    usersToRemove.Add(config);
                }
            }

            // Xóa quyền
            foreach (var config in usersToRemove)
            {
                _unitOfWork.UserAiConfigs.Delete(config);
            }

            return usersToRemove.Count;
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var userAiConfig = await _unitOfWork.UserAiConfigs.GetByUserAndAIAsync(userId, aiConfigureId);
                if (userAiConfig == null)
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết");
                }

                // Xóa cascade quyền của user cấp dưới
                var cascadeRemoved = await DeleteCascadeAccessAsync(userId, aiConfigureId);

                // Xóa quyền của user này
                _unitOfWork.UserAiConfigs.Delete(userAiConfig);
                await _unitOfWork.SaveChangesAsync();

                var message = cascadeRemoved > 0
                    ? $"Xóa liên kết thành công. Đã xóa cascade quyền của {cascadeRemoved} user(s) cấp dưới"
                    : "Xóa liên kết thành công";

                return ApiResponse<bool>.Ok(true, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> HasAccessAsync(Guid userId, Guid aiConfigureId)
        {
            try
            {
                var hasAccess = await _unitOfWork.UserAiConfigs.HasAccessAsync(userId, aiConfigureId);
                return ApiResponse<bool>.Ok(hasAccess, hasAccess ? "Người dùng có quyền truy cập" : "Người dùng không có quyền truy cập");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi kiểm tra quyền truy cập: {ex.Message}");
            }
        }
    }
}
