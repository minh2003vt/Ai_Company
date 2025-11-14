using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public Guid CompanyId { get; set; }

    }

    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public Guid CompanyId { get; set; }

    }

    public class DepartmentResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
    }
}
