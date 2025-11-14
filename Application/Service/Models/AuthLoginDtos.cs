using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; }
        public System.Guid Id { get; set; }
        public string FullName { get; set; }
        public System.Guid? Companyid { get; set; }
        public string? CompanyName { get; set; }
        public string Email { get; set; }
        public List<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();
    }


    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Nhập lại mật khẩu là bắt buộc")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; }
    }
}


