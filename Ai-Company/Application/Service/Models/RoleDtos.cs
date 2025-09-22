using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class RoleCreateDto
    {
        [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên vai trò không được vượt quá 100 ký tự")]
        public string Name { get; set; }
    }

    public class RoleUpdateDto
    {
        [Required(ErrorMessage = "Tên vai trò là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên vai trò không được vượt quá 100 ký tự")]
        public string Name { get; set; }
    }

    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
