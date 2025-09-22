using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public Guid CompanyId { get; set; }
    }

    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên phòng ban không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "ID công ty là bắt buộc")]
        public Guid CompanyId { get; set; }
    }

    public class DepartmentResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int UserCount { get; set; }
    }
}
