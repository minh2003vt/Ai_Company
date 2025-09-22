using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class CompanyCreateDto
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
        [MaxLength(15, ErrorMessage = "Mã số thuế không được vượt quá 15 ký tự")]
        public string TIN { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string Description { get; set; }
    }

    public class CompanyUpdateDto
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Mã số thuế là bắt buộc")]
        [MaxLength(15, ErrorMessage = "Mã số thuế không được vượt quá 15 ký tự")]
        public string TIN { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string Description { get; set; }
    }

    public class CompanyResponseDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string TIN { get; set; }
        public string Description { get; set; }
        public int DepartmentCount { get; set; }
        public int UserCount { get; set; }
    }
}
