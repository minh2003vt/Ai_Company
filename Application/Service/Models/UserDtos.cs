using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class UserMeDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public List<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
    }

    public class UpdateUserMeDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Phone]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public DateTime DateOfBirth { get; set; }
    }

    public class CreateUserDto
    {
        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(150, ErrorMessage = "Email không được vượt quá 150 ký tự")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime DateOfBirth { get; set; }


        [Required(ErrorMessage = "Company ID là bắt buộc")]
        public Guid CompanyId { get; set; }
    }

    public class UserResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public List<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserRoleDto
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
    }
}
