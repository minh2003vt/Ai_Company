using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites
{
    public class AIModelConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string ModelName { get; set; } = "gemini-2.5-pro";

        [Required]
        public float Temperature { get; set; } = 0.85f;

        [Required]
        public int MaxOutputTokens { get; set; } = 8192;

        [Required]
        public bool UseStreaming { get; set; } = false;

        // API Key for the model (encrypted or plain text - can be encrypted later)
        // Nếu null, service sẽ fallback về config từ appsettings
        [MaxLength(500)]
        public string? ApiKey { get; set; }

        // Password (hashed) for additional security
        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        // Optional: Additional model parameters
        public float? TopP { get; set; }
        public int? TopK { get; set; }

        // Active flag - only one model can be active at a time
        public bool Active { get; set; } = false;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
        public DateTime? UpdatedAt { get; set; }

        // Navigation property - one config can be used by multiple AI_Configure
        public virtual ICollection<AI_Configure> AI_Configures { get; set; } = new List<AI_Configure>();
    }
}

