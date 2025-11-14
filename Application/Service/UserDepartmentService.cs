using Application.Service.Interfaces;
using Application.Service.Models;
using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Service
{
    public class UserDepartmentService : IUserDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserDepartmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<UserDepartmentResponseDto>> CreateAsync(UserDepartmentCreateDto dto)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
                if (user == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng không tồn tại");
                }

                // Kiểm tra department có tồn tại không (nếu có)
                Department? department = null;
                if (dto.DepartmentId.HasValue)
                {
                    department = await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId.Value);
                    if (department == null)
                    {
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Phòng ban không tồn tại");
                    }
                }

                // Kiểm tra role có tồn tại không
                var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (role == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Role không tồn tại");
                }

                // Kiểm tra SystemAdmin role không được phép chọn
                if (role.Name.ToLower() == "systemadmin")
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Không được phép chọn role SystemAdmin");
                }

                // Kiểm tra user và department có cùng company không
                if (dto.DepartmentId.HasValue && user.CompanyId != department!.CompanyId)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng và phòng ban phải thuộc cùng công ty");
                }

                // Validation cho Manager limit (1 department chỉ được có maximum 5 manager)
                if (role.Name.ToLower() == "manager" && dto.DepartmentId.HasValue)
                {
                    var managerCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                        ud.DepartmentId == dto.DepartmentId && 
                        ud.Role.Name.ToLower() == "manager");
                    
                    if (managerCount >= 5)
                    {
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Phòng ban đã đạt giới hạn 5 manager");
                    }

                    // Validation: 1 user chỉ có thể là Manager của 1 department duy nhất
                    var existingManagerRole = await _unitOfWork.UserDepartments.FindAsync(ud => 
                        ud.UserId == dto.UserId && 
                        ud.Role.Name.ToLower() == "manager" &&
                        ud.DepartmentId.HasValue &&
                        ud.DepartmentId != dto.DepartmentId);
                    
                    if (existingManagerRole.Any())
                    {
                        var existingDept = await _unitOfWork.Departments.GetByIdAsync(existingManagerRole.First().DepartmentId!.Value);
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, 
                            $"Người dùng này đã là Manager của phòng ban '{existingDept?.Name ?? "khác"}'. Một người dùng chỉ có thể làm Manager của 1 phòng ban.");
                    }
                }

                // Validation cho Admin limit (1 company chỉ được có 5 admin)
                if (role.Name.ToLower() == "admin")
                {
                    var companyId = dto.DepartmentId.HasValue ? department!.CompanyId : user.CompanyId;
                    if (companyId.HasValue)
                    {
                        var adminCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                            ud.User.CompanyId == companyId && 
                            ud.Role.Name.ToLower() == "admin");
                        
                        if (adminCount >= 5)
                        {
                            return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Công ty đã đạt giới hạn 5 admin");
                        }
                    }
                }

                // Kiểm tra user đã có trong department này chưa
                var existingUserDepartment = await _unitOfWork.UserDepartments.FindAsync(ud => 
                    ud.UserId == dto.UserId && ud.DepartmentId == dto.DepartmentId);
                
                if (existingUserDepartment.Any())
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng đã có trong phòng ban này");
                }

                var userDepartment = new UserDepartment
                {
                    UserId = dto.UserId,
                    DepartmentId = dto.DepartmentId,
                    RoleId = dto.RoleId
                };

                await _unitOfWork.UserDepartments.AddAsync(userDepartment);
                await _unitOfWork.SaveChangesAsync();

                // Lấy thông tin để trả về
                var company = await _unitOfWork.Companies.GetByIdAsync(user.CompanyId ?? Guid.Empty);
                var response = new UserDepartmentResponseDto
                {
                    UserId = userDepartment.UserId,
                    UserName = user.FullName,
                    UserEmail = user.Email,
                    DepartmentId = userDepartment.DepartmentId,
                    DepartmentName = department?.Name,
                    RoleId = userDepartment.RoleId,
                    RoleName = role.Name,
                    CompanyId = company?.Id ?? Guid.Empty,
                    CompanyName = company?.CompanyName ?? "Unknown"
                };

                return ApiResponse<UserDepartmentResponseDto>.Ok(response, "Tạo liên kết người dùng - phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDepartmentResponseDto>.Fail(null, $"Lỗi khi tạo liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDepartmentResponseDto>> UpdateAsync(Guid userId, Guid departmentId, UserDepartmentUpdateDto dto)
        {
            try
            {
                var userDepartment = await _unitOfWork.UserDepartments.FindAsync(ud => 
                    ud.UserId == userId && ud.DepartmentId == departmentId);
                
                if (!userDepartment.Any())
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Không tìm thấy liên kết người dùng - phòng ban");
                }

                var userDept = userDepartment.First();

                // Kiểm tra role có tồn tại không
                var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (role == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Role không tồn tại");
                }

                // Kiểm tra SystemAdmin role không được phép chọn
                if (role.Name.ToLower() == "systemadmin")
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Không được phép chọn role SystemAdmin");
                }

                // Validation cho Manager limit
                if (role.Name.ToLower() == "manager")
                {
                    var managerCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                        ud.DepartmentId == departmentId && 
                        ud.Role.Name.ToLower() == "manager" &&
                        !(ud.UserId == userId && ud.DepartmentId == departmentId));
                    
                    if (managerCount >= 5)
                    {
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Phòng ban đã đạt giới hạn 5 manager");
                    }

                    // Validation: 1 user chỉ có thể là Manager của 1 department duy nhất
                    // Nếu đang update sang Manager role, check xem user đã có Manager role ở department khác chưa
                    var existingManagerRole = await _unitOfWork.UserDepartments.FindAsync(ud => 
                        ud.UserId == userId && 
                        ud.Role.Name.ToLower() == "manager" &&
                        ud.DepartmentId.HasValue &&
                        ud.DepartmentId != departmentId);
                    
                    if (existingManagerRole.Any())
                    {
                        var existingDept = await _unitOfWork.Departments.GetByIdAsync(existingManagerRole.First().DepartmentId!.Value);
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, 
                            $"Người dùng này đã là Manager của phòng ban '{existingDept?.Name ?? "khác"}'. Một người dùng chỉ có thể làm Manager của 1 phòng ban.");
                    }
                }

                // Validation cho Admin limit
                if (role.Name.ToLower() == "admin")
                {
                    var department = await _unitOfWork.Departments.GetByIdAsync(departmentId);
                    if (department != null)
                    {
                        var adminCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                            ud.User.CompanyId == department.CompanyId && 
                            ud.Role.Name.ToLower() == "admin" &&
                            !(ud.UserId == userId && ud.DepartmentId == departmentId));
                        
                        if (adminCount >= 5)
                        {
                            return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Công ty đã đạt giới hạn 5 admin");
                        }
                    }
                }

                userDept.RoleId = dto.RoleId;
                _unitOfWork.UserDepartments.Update(userDept);
                await _unitOfWork.SaveChangesAsync();

                // Lấy thông tin để trả về
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                var dept = await _unitOfWork.Departments.GetByIdAsync(departmentId);
                var company = await _unitOfWork.Companies.GetByIdAsync(dept?.CompanyId ?? Guid.Empty);
                
                var response = new UserDepartmentResponseDto
                {
                    UserId = userDept.UserId,
                    UserName = user?.FullName ?? "Unknown",
                    UserEmail = user?.Email ?? "Unknown",
                    DepartmentId = userDept.DepartmentId,
                    DepartmentName = dept?.Name,
                    RoleId = userDept.RoleId,
                    RoleName = role.Name,
                    CompanyId = company?.Id ?? Guid.Empty,
                    CompanyName = company?.CompanyName ?? "Unknown"
                };

                return ApiResponse<UserDepartmentResponseDto>.Ok(response, "Cập nhật liên kết thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDepartmentResponseDto>.Fail(null, $"Lỗi khi cập nhật liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid departmentId)
        {
            try
            {
                var userDepartment = await _unitOfWork.UserDepartments.FindAsync(ud => 
                    ud.UserId == userId && ud.DepartmentId == departmentId);
                
                if (!userDepartment.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết người dùng - phòng ban");
                }

                _unitOfWork.UserDepartments.Delete(userDepartment.First());
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, "Xóa liên kết thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteByUserIdAsync(Guid userId)
        {
            try
            {
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
                
                if (!userDepartments.Any())
                {
                    return ApiResponse<bool>.Fail(false, "Không tìm thấy liên kết nào của người dùng này");
                }

                foreach (var userDept in userDepartments)
                {
                    _unitOfWork.UserDepartments.Delete(userDept);
                }

                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true, $"Xóa {userDepartments.Count()} liên kết của người dùng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Fail(false, $"Lỗi khi xóa liên kết: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByDepartmentAsync(Guid departmentId)
        {
            try
            {
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.DepartmentId == departmentId);
                var response = new List<UserDepartmentResponseDto>();

                foreach (var ud in userDepartments)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(ud.UserId);
                    var department = await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId ?? Guid.Empty);
                    var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                    var company = await _unitOfWork.Companies.GetByIdAsync(department?.CompanyId ?? Guid.Empty);

                    response.Add(new UserDepartmentResponseDto
                    {
                        UserId = ud.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        DepartmentId = ud.DepartmentId,
                        DepartmentName = department?.Name,
                        RoleId = ud.RoleId,
                        RoleName = role?.Name ?? "Unknown",
                        CompanyId = company?.Id ?? Guid.Empty,
                        CompanyName = company?.CompanyName ?? "Unknown"
                    });
                }

                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Ok(response, "Lấy danh sách người dùng theo phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByUserAsync(Guid userId)
        {
            try
            {
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => ud.UserId == userId);
                var response = new List<UserDepartmentResponseDto>();

                foreach (var ud in userDepartments)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(ud.UserId);
                    var department = await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId ?? Guid.Empty);
                    var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                    var company = await _unitOfWork.Companies.GetByIdAsync(department?.CompanyId ?? Guid.Empty);

                    response.Add(new UserDepartmentResponseDto
                    {
                        UserId = ud.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        DepartmentId = ud.DepartmentId,
                        DepartmentName = department?.Name,
                        RoleId = ud.RoleId,
                        RoleName = role?.Name ?? "Unknown",
                        CompanyId = company?.Id ?? Guid.Empty,
                        CompanyName = company?.CompanyName ?? "Unknown"
                    });
                }

                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Ok(response, "Lấy danh sách phòng ban theo người dùng thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách: {ex.Message}");
            }
        }

        public async Task<ApiResponse<IEnumerable<UserDepartmentResponseDto>>> GetByCompanyAsync(Guid companyId)
        {
            try
            {
                // Lấy tất cả user trong company
                var users = await _unitOfWork.Users.FindAsync(u => u.CompanyId == companyId);
                var userIds = users.Select(u => u.Id).ToList();

                // Lấy tất cả UserDepartment của các user trong company
                var userDepartments = await _unitOfWork.UserDepartments.FindAsync(ud => userIds.Contains(ud.UserId));
                var response = new List<UserDepartmentResponseDto>();

                foreach (var ud in userDepartments)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(ud.UserId);
                    var department = await _unitOfWork.Departments.GetByIdAsync(ud.DepartmentId ?? Guid.Empty);
                    var role = await _unitOfWork.Roles.GetByIdAsync(ud.RoleId);
                    var company = await _unitOfWork.Companies.GetByIdAsync(companyId);

                    response.Add(new UserDepartmentResponseDto
                    {
                        UserId = ud.UserId,
                        UserName = user?.FullName ?? "Unknown",
                        UserEmail = user?.Email ?? "Unknown",
                        DepartmentId = ud.DepartmentId,
                        DepartmentName = department?.Name,
                        RoleId = ud.RoleId,
                        RoleName = role?.Name ?? "Unknown",
                        CompanyId = company?.Id ?? Guid.Empty,
                        CompanyName = company?.CompanyName ?? "Unknown"
                    });
                }

                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Ok(response, "Lấy danh sách liên kết theo công ty thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<UserDepartmentResponseDto>>.Fail(null, $"Lỗi khi lấy danh sách: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserDepartmentResponseDto>> AssignUserToDepartmentAsync(AssignUserToDepartmentDto dto)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
                if (user == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng không tồn tại");
                }

                // Kiểm tra department có tồn tại không
                var department = await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId);
                if (department == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Phòng ban không tồn tại");
                }

                // Kiểm tra user và department có cùng company không
                if (user.CompanyId != department.CompanyId)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng và phòng ban phải thuộc cùng công ty");
                }

                // Kiểm tra role có tồn tại không
                var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (role == null)
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Role không tồn tại");
                }

                // Kiểm tra SystemAdmin role không được phép chọn
                if (role.Name.ToLower() == "systemadmin")
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Không được phép chọn role SystemAdmin");
                }

                // Validation cho Manager limit
                if (role.Name.ToLower() == "manager")
                {
                    var managerCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                        ud.DepartmentId == dto.DepartmentId && 
                        ud.Role.Name.ToLower() == "manager");
                    
                    if (managerCount >= 5)
                    {
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Phòng ban đã đạt giới hạn 5 manager");
                    }

                    // Validation: 1 user chỉ có thể là Manager của 1 department duy nhất
                    var existingManagerRole = await _unitOfWork.UserDepartments.FindAsync(ud => 
                        ud.UserId == dto.UserId && 
                        ud.Role.Name.ToLower() == "manager" &&
                        ud.DepartmentId.HasValue &&
                        ud.DepartmentId != dto.DepartmentId);
                    
                    if (existingManagerRole.Any())
                    {
                        var existingDept = await _unitOfWork.Departments.GetByIdAsync(existingManagerRole.First().DepartmentId!.Value);
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, 
                            $"Người dùng này đã là Manager của phòng ban '{existingDept?.Name ?? "khác"}'. Một người dùng chỉ có thể làm Manager của 1 phòng ban.");
                    }
                }

                // Validation cho Admin limit
                if (role.Name.ToLower() == "admin")
                {
                    var adminCount = await _unitOfWork.UserDepartments.CountAsync(ud => 
                        ud.User.CompanyId == department.CompanyId && 
                        ud.Role.Name.ToLower() == "admin");
                    
                    if (adminCount >= 5)
                    {
                        return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Công ty đã đạt giới hạn 5 admin");
                    }
                }

                // Kiểm tra user đã có trong department này chưa
                var existingUserDepartment = await _unitOfWork.UserDepartments.FindAsync(ud => 
                    ud.UserId == dto.UserId && ud.DepartmentId == dto.DepartmentId);
                
                if (existingUserDepartment.Any())
                {
                    return ApiResponse<UserDepartmentResponseDto>.Fail(null, "Người dùng đã có trong phòng ban này");
                }

                var userDepartment = new UserDepartment
                {
                    UserId = dto.UserId,
                    DepartmentId = dto.DepartmentId,
                    RoleId = dto.RoleId
                };

                await _unitOfWork.UserDepartments.AddAsync(userDepartment);
                await _unitOfWork.SaveChangesAsync();

                // Lấy thông tin để trả về
                var company = await _unitOfWork.Companies.GetByIdAsync(department.CompanyId);
                var response = new UserDepartmentResponseDto
                {
                    UserId = userDepartment.UserId,
                    UserName = user.FullName,
                    UserEmail = user.Email,
                    DepartmentId = userDepartment.DepartmentId,
                    DepartmentName = department.Name,
                    RoleId = userDepartment.RoleId,
                    RoleName = role.Name,
                    CompanyId = company?.Id ?? Guid.Empty,
                    CompanyName = company?.CompanyName ?? "Unknown"
                };

                return ApiResponse<UserDepartmentResponseDto>.Ok(response, "Gán người dùng vào phòng ban thành công");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserDepartmentResponseDto>.Fail(null, $"Lỗi khi gán người dùng: {ex.Message}");
            }
        }
    }
}
