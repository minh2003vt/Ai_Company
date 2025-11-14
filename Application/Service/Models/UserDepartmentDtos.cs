using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class UserDepartmentCreateDto
    {
        [Required(ErrorMessage = "User ID là bắt buộc")]
        public Guid UserId { get; set; }

        public Guid? DepartmentId { get; set; }

        [Required(ErrorMessage = "Role ID là bắt buộc")]
        public Guid RoleId { get; set; }
    }

    public class UserDepartmentUpdateDto
    {
        [Required(ErrorMessage = "Role ID là bắt buộc")]
        public Guid RoleId { get; set; }
    }

    public class UserDepartmentResponseDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; }
    }

    public class AssignUserToDepartmentDto
    {
        [Required(ErrorMessage = "User ID là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Department ID là bắt buộc")]
        public Guid DepartmentId { get; set; }

        [Required(ErrorMessage = "Role ID là bắt buộc")]
        public Guid RoleId { get; set; }
    }
}
