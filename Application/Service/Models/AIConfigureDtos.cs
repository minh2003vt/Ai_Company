using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    // DTOs for AIModelConfig
    public class AIModelConfigDto
    {
        [Required, MaxLength(200)]
        public string ModelName { get; set; } = "gemini-2.5-pro";

        [Required]
        public float Temperature { get; set; } = 0.85f;

        [Required]
        public int MaxOutputTokens { get; set; } = 8192;

        [Required]
        public bool UseStreaming { get; set; } = false;

        // ApiKey - chỉ dùng khi Create,  dùng tkhôngrong Update (dùng hàm update api-key riêng)
        [MaxLength(500)]
        public string? ApiKey { get; set; }

        public float? TopP { get; set; }
        public int? TopK { get; set; }
    }

    public class AIModelConfigResponseDto
    {
        public Guid Id { get; set; }
        public string ModelName { get; set; }
        public float Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
        public bool UseStreaming { get; set; }
        public float? TopP { get; set; }
        public int? TopK { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateModelConfigPasswordDto
    {
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải 6 ký tự")]
        [MaxLength(6, ErrorMessage = "Mật khẩu phải 6 ký tự")]
        public string NewPassword { get; set; }
    }

    public class GetModelConfigByIdDto
    {
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; }
    }

    public class ModelConfigApiKeyResponseDto
    {
        public Guid Id { get; set; }
        public string ApiKey { get; set; }
    }

    public class UpdateModelConfigApiKeyDto
    {
        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; }

        [Required(ErrorMessage = "ApiKey mới là bắt buộc")]
        [MaxLength(500, ErrorMessage = "ApiKey không được vượt quá 500 ký tự")]
        public string NewApiKey { get; set; }
    }

    public class AIConfigureDto
    {
        [Required(ErrorMessage = "Tên cấu hình AI là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên cấu hình AI không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }
        // Quy tắc bổ sung (tùy chọn). Mặc định sẽ luôn có: "only answer information in the RAG"
        [MaxLength(2000)]
        public string Rules { get; set; }

        // Optional version name for initial snapshot (e.g., "v1.0.0")
        [MaxLength(50)]
        public string? Version { get; set; }

        // Model configuration - either provide ModelConfigId or ModelConfig object
        public Guid? ModelConfigId { get; set; }
        public AIModelConfigDto? ModelConfig { get; set; }
    }
    public class AIConfigureUpdateDto
    {
        [Required(ErrorMessage = "Tên cấu hình AI là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên cấu hình AI không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }
        // Quy tắc bổ sung (tùy chọn). Mặc định sẽ luôn có: "only answer information in the RAG"
        [MaxLength(2000)]
        public string Rules { get; set; }

        // Optional new version name for the update (e.g., "v1.0.1")
        [MaxLength(50)]
        public string? Version { get; set; }

        // Model configuration - either provide ModelConfigId or ModelConfig object
        public Guid? ModelConfigId { get; set; }
        public AIModelConfigDto? ModelConfig { get; set; }
    }

    public class AIConfigureResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string? CurrentVersion { get; set; }
    }
    public class AIConfigureDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rules { get; set; }
        public int RagTopK { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public int KnowledgeSourceCount { get; set; }
        public int ChatSessionCount { get; set; }
        public string? CurrentVersion { get; set; }
        public AIModelConfigResponseDto ModelConfig { get; set; }
    }

    public class GrantChatAccessDto
    {
        [Required]
        public Guid TargetUserId { get; set; }

        public bool Revoke { get; set; } = false;
    }

    public class GrantCompanyAccessDto
    {
        [Required]
        public Guid TargetCompanyId { get; set; }

        public bool Revoke { get; set; } = false;
    }

    public class GrantDepartmentAccessDto
    {
        [Required]
        public Guid TargetDepartmentId { get; set; }

        public bool Revoke { get; set; } = false;
    }

    
}
