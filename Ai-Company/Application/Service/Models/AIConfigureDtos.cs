using System.ComponentModel.DataAnnotations;

namespace Application.Service.Models
{
    public class AIConfigureCreateDto
    {
        [Required(ErrorMessage = "Tên cấu hình AI là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên cấu hình AI không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }

        public string RulesJson { get; set; }

        [MaxLength(200, ErrorMessage = "Tên model không được vượt quá 200 ký tự")]
        public string ModelName { get; set; } = "gemini-1.5-pro";

        [Range(0.0f, 2.0f, ErrorMessage = "Temperature phải từ 0.0 đến 2.0")]
        public float Temperature { get; set; } = 0.0f;

        [Range(1, 8192, ErrorMessage = "MaxOutputTokens phải từ 1 đến 8192")]
        public int MaxOutputTokens { get; set; } = 512;

        public bool UseStreaming { get; set; } = false;

        public bool UseRag { get; set; } = true;

        [Range(1, 50, ErrorMessage = "RagTopK phải từ 1 đến 50")]
        public int RagTopK { get; set; } = 5;
    }

    public class AIConfigureUpdateDto
    {
        [Required(ErrorMessage = "Tên cấu hình AI là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Tên cấu hình AI không được vượt quá 100 ký tự")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; }

        public string RulesJson { get; set; }

        [MaxLength(200, ErrorMessage = "Tên model không được vượt quá 200 ký tự")]
        public string ModelName { get; set; } = "gemini-1.5-pro";

        [Range(0.0f, 2.0f, ErrorMessage = "Temperature phải từ 0.0 đến 2.0")]
        public float Temperature { get; set; } = 0.0f;

        [Range(1, 8192, ErrorMessage = "MaxOutputTokens phải từ 1 đến 8192")]
        public int MaxOutputTokens { get; set; } = 512;

        public bool UseStreaming { get; set; } = false;

        public bool UseRag { get; set; } = true;

        [Range(1, 50, ErrorMessage = "RagTopK phải từ 1 đến 50")]
        public int RagTopK { get; set; } = 5;
    }

    public class AIConfigureResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RulesJson { get; set; }
        public string ModelName { get; set; }
        public float Temperature { get; set; }
        public int MaxOutputTokens { get; set; }
        public bool UseStreaming { get; set; }
        public bool UseRag { get; set; }
        public int RagTopK { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public int KnowledgeSourceCount { get; set; }
        public int ChatSessionCount { get; set; }
    }
}
