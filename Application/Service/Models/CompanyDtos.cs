using System.ComponentModel.DataAnnotations;
using Domain.Entitites.Enums;

namespace Application.Service.Models
{
    public class CompanyCreateDto
    {
        [Required(ErrorMessage = "Mã công ty là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mã công ty không được vượt quá 100 ký tự")]
        public string CompanyCode { get; set; }

        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string Description { get; set; }

        [MaxLength(200, ErrorMessage = "Website không được vượt quá 200 ký tự")]
        public string? Website { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng người dùng tối đa phải từ 1 đến 1000")]
        public int MaximumUser { get; set; } = 10;
        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.OneMonth;
        public CompanyStatus Status { get; set; } = CompanyStatus.Active;
    }

    public class CompanyUpdateDto
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Mô tả không được vượt quá 100 ký tự")]
        public string Description { get; set; }

        [MaxLength(200, ErrorMessage = "Website không được vượt quá 200 ký tự")]
        public string? Website { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng người dùng tối đa phải từ 1 đến 1000")]
        public int MaximumUser { get; set; } = 10;

        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.OneMonth;
    }

    public class CompanyStatusUpdateDto
    {
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public CompanyStatus Status { get; set; }
    }

    public class CompanyBasicDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }
        public string? Website { get; set; }
        public CompanyStatus Status { get; set; }
        public int DepartmentCount { get; set; }
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int StaffCount { get; set; }
    }

    public class CompanyResponseDto
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string CompanyCode { get; set; }
        public string Description { get; set; }
        public string? Website { get; set; }
        public int MaximumUser { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public DateTime StartSubscriptionDate { get; set; }
        public CompanyStatus Status { get; set; }
        public int DepartmentCount { get; set; }
        public int AdminCount { get; set; }
        public int ManagerCount { get; set; }
        public int StaffCount { get; set; }
    }
}
