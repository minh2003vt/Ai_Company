using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class UserAiConfigCreateDto
    {
        [Required(ErrorMessage = "User ID là bắt buộc")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "AI Configure ID là bắt buộc")]
        public Guid AIConfigureId { get; set; }
    }


    public class UserAiConfigResponseDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public Guid AIConfigureId { get; set; }
        public string AIConfigureName { get; set; }
        public string AIConfigureDescription { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserAiConfigDetailResponseDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public Guid AIConfigureId { get; set; }
        public string AIConfigureName { get; set; }
        public string AIConfigureDescription { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
