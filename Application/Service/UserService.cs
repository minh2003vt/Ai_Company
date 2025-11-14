using Application.Service.Interfaces;
using Application.Service.Models;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Security;
using Domain.Entitites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public UserService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<ApiResponse<UserMeDto>> GetMeAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserMeDto>.Fail(null, "Người dùng không tồn tại");
            }

            // Lấy roles từ UserDepartment
            var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
            var roles = new List<UserRoleDto>();
            
            foreach (var ud in userDepartments)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                var department = ud.DepartmentId.HasValue ? await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId.Value) : null;
                
                roles.Add(new UserRoleDto
                {
                    RoleId = ud.RoleId,
                    RoleName = role?.Name ?? "Unknown",
                    DepartmentId = ud.DepartmentId,
                    DepartmentName = department?.Name
                });
            }

            var companyName = user.Company?.CompanyName;
            if (string.IsNullOrEmpty(companyName))
            {
                var c = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId ?? Guid.Empty);
                companyName = c?.CompanyName;
            }

            var dto = new UserMeDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Roles = roles,
                CompanyId = user.CompanyId,
                CompanyName = companyName
            };

            return ApiResponse<UserMeDto>.Ok(dto, "Lấy thông tin người dùng thành công");
        }

        public async Task<ApiResponse<UserMeDto>> UpdateMeAsync(Guid userId, UpdateUserMeDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserMeDto>.Fail(null, "Người dùng không tồn tại");
            }

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc);
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Lấy roles từ UserDepartment
            var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == user.Id);
            var roles = new List<UserRoleDto>();
            
            foreach (var ud in userDepartments)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                var dept = ud.DepartmentId.HasValue ? await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId.Value) : null;
                
                roles.Add(new UserRoleDto
                {
                    RoleId = ud.RoleId,
                    RoleName = role?.Name ?? "Unknown",
                    DepartmentId = ud.DepartmentId,
                    DepartmentName = dept?.Name
                });
            }

            var companyName = user.Company?.CompanyName;
            if (string.IsNullOrEmpty(companyName))
            {
                var c = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId ?? Guid.Empty);
                companyName = c?.CompanyName;
            }

            var result = new UserMeDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Roles = roles,
                CompanyId = user.CompanyId,
                CompanyName = companyName
            };

            return ApiResponse<UserMeDto>.Ok(result, "Cập nhật thông tin người dùng thành công");
        }

        public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetUsersByCompanyAsync(Guid companyId)
        {
            try
            {
                var users = await _unitOfWork.Users.FindAsync(u => u.CompanyId == companyId);
                var response = new List<UserResponseDto>();

                foreach (var user in users)
                {
                    // Lấy roles từ UserDepartment
                    var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == user.Id);
                    var roles = new List<UserRoleDto>();
                    
                    foreach (var ud in userDepartments)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                        var dept = ud.DepartmentId.HasValue ? await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId.Value) : null;
                        
                        roles.Add(new UserRoleDto
                        {
                            RoleId = ud.RoleId,
                            RoleName = role?.Name ?? "Unknown",
                            DepartmentId = ud.DepartmentId,
                            DepartmentName = dept?.Name
                        });
                    }

                    var companyName = user.Company?.CompanyName;
                    if (string.IsNullOrEmpty(companyName))
                    {
                        var c = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId ?? Guid.Empty);
                        companyName = c?.CompanyName;
                    }

                    response.Add(new UserResponseDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        Roles = roles,
                        CompanyId = user.CompanyId,
                        CompanyName = companyName,
                        CreatedAt = user.CreatedAt
                    });
                }

                return ApiResponse<IEnumerable<UserResponseDto>>.Ok(response, "Lấy danh sách người dùng theo công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách người dùng: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetUsersByDepartmentAsync(Guid departmentId)
        {
            try
            {
                // Lấy department để lấy companyId
                var department = await _unitOfWork.Departments.GetByIdAsync(departmentId);
                if (department == null)
                {
                    return ApiResponse<IEnumerable<UserResponseDto>>.Fail(null, "Phòng ban không tồn tại");
                }

                // Lấy tất cả UserDepartments có DepartmentId = departmentId
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.DepartmentId == departmentId);
                
                // Lấy danh sách unique UserIds
                var userIds = userDepartments.Select(ud => ud.UserId).Distinct().ToList();
                
                if (!userIds.Any())
                {
                    return ApiResponse<IEnumerable<UserResponseDto>>.Ok(new List<UserResponseDto>(), "Không có người dùng nào trong phòng ban này");
                }

                // Lấy users theo UserIds
                var users = await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id));
                var response = new List<UserResponseDto>();

                foreach (var user in users)
                {
                    // Chỉ lấy roles có DepartmentId = departmentId cho user này
                    var userDeptsForThisDept = userDepartments.Where(ud => ud.UserId == user.Id).ToList();
                    var roles = new List<UserRoleDto>();
                    
                    foreach (var ud in userDeptsForThisDept)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                        var dept = ud.DepartmentId.HasValue ? await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId.Value) : null;
                        
                        roles.Add(new UserRoleDto
                        {
                            RoleId = ud.RoleId,
                            RoleName = role?.Name ?? "Unknown",
                            DepartmentId = ud.DepartmentId,
                            DepartmentName = dept?.Name
                        });
                    }

                    var companyName = user.Company?.CompanyName;
                    if (string.IsNullOrEmpty(companyName))
                    {
                        var c = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId ?? Guid.Empty);
                        companyName = c?.CompanyName;
                    }

                    response.Add(new UserResponseDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        DateOfBirth = user.DateOfBirth,
                        Roles = roles,
                        CompanyId = user.CompanyId,
                        CompanyName = companyName,
                        CreatedAt = user.CreatedAt
                    });
                }

                return ApiResponse<IEnumerable<UserResponseDto>>.Ok(response, "Lấy danh sách người dùng theo phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách người dùng: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserDto dto)
        {
            try
            {
                // Kiểm tra email đã tồn tại chưa
                var existingUser = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email);
                if (existingUser.Any())
                {
                    return ApiResponse<UserResponseDto>.Fail(null, "Email đã tồn tại");
                }

                // Kiểm tra company có tồn tại không
                var company = await _unitOfWork.Companies.GetByIdAsync(dto.CompanyId);
                if (company == null)
                {
                    return ApiResponse<UserResponseDto>.Fail(null, "Công ty không tồn tại");
                }

                // Kiểm tra company có đạt giới hạn user không
                var currentUserCount = await _unitOfWork.Users.CountAsync(u => u.CompanyId == dto.CompanyId);
                if (currentUserCount >= company.MaximumUser)
                {
                    return ApiResponse<UserResponseDto>.Fail(null, $"Công ty đã đạt giới hạn {company.MaximumUser} người dùng");
            }

            // Hash password
                var passwordHash = PasswordHasher.HashPassword(dto.Password);

                var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc),
                    CompanyId = dto.CompanyId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

                // Gửi email chứa password cho user mới
                try
                {
                    var emailSubject = $"Chào mừng đến với {company.CompanyName} - Thông tin đăng nhập";
                    var emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                                <h2 style='color: #2c3e50;'>Chào mừng {user.FullName}!</h2>
                                
                                <p>Bạn đã được tạo tài khoản thành công trong hệ thống <strong>{company.CompanyName}</strong>.</p>
                                
                                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                    <h3 style='color: #2c3e50; margin-top: 0;'>Thông tin đăng nhập:</h3>
                                    <p><strong>Email:</strong> {user.Email}</p>
                                    <p><strong>Mật khẩu:</strong> <span style='background-color: #e9ecef; padding: 2px 6px; border-radius: 3px; font-family: monospace;'>{dto.Password}</span></p>
                                    <p><strong>Lưu ý:</strong> Vai trò sẽ được gán sau khi tài khoản được tạo</p>
                                </div>
                                
                                <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                    <h4 style='color: #0c5460; margin-top: 0;'>⚠️ Lưu ý bảo mật:</h4>
                                    <ul style='margin: 10px 0; padding-left: 20px;'>
                                        <li>Vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên</li>
                                        <li>Không chia sẻ thông tin đăng nhập với người khác</li>
                                        <li>Liên hệ quản trị viên nếu có vấn đề</li>
                                    </ul>
                                </div>
                                
                                <p>Chúc bạn có trải nghiệm tốt với hệ thống!</p>
                                
                                <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                                <p style='font-size: 12px; color: #666;'>
                                    Email này được gửi tự động từ hệ thống {company.CompanyName}.<br>
                                    Vui lòng không trả lời email này.
                                </p>
                            </div>
                        </body>
                        </html>";

                    await _emailService.SendAsync(user.Email, emailSubject, emailBody);
                }
                catch (Exception emailEx)
                {
                    // Log lỗi gửi email nhưng không fail việc tạo user
                    Console.WriteLine($"Lỗi gửi email cho user {user.Email}: {emailEx.Message}");
                }

                // Tạo response (không có role vì chưa được assign)
                var response = new UserResponseDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Roles = new List<UserRoleDto>(), // Chưa có role, cần assign qua UserDepartment
                    CompanyId = company.Id,
                    CompanyName = company.CompanyName,
                    CreatedAt = user.CreatedAt
                };

                return ApiResponse<UserResponseDto>.Ok(response, "Tạo người dùng thành công và đã gửi email thông tin đăng nhập. Vui lòng assign role cho user này.");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserResponseDto>.Fail(null, $"Lỗi khi tạo người dùng: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteUserAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.Fail(false, "Người dùng không tồn tại");
                }

                // Xóa tất cả UserDepartment liên kết với user này
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
                foreach (var userDept in userDepartments)
                {
                    _unitOfWork.UserDepartments.Delete(userDept);
                }

                // Xóa tất cả UserAiConfig liên kết với user này
                var userAiConfigs = await _unitOfWork.UserAiConfigs.FindAsync(uac => uac.UserId == userId);
                foreach (var userAiConfig in userAiConfigs)
                {
                    _unitOfWork.UserAiConfigs.Delete(userAiConfig);
                }

                // Xóa user
                _unitOfWork.Users.Delete(user);
            await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa người dùng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa người dùng: {ex.Message}");
            }
        }
    }
}
