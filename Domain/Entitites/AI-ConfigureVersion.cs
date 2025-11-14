using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entitites.Enums;

namespace Domain.Entitites
{
    public class AI_ConfigureVersion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid AIConfigureId { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }

        // Version name per AIConfigureId (e.g., "v1.0.1")
        [Required, MaxLength(50)]
        public string Version { get; set; }

        // Snapshot fields
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(255)]
        public string Description { get; set; }

        [Required, MaxLength(2000)]
        public string Rules { get; set; }

        // Snapshot ModelConfigId instead of individual fields
        [Required]
        public Guid ModelConfigId { get; set; }

        public int RagTopK { get; set; }

        [Required]
        public AI_ConfigureKind Kind { get; set; }

        public Guid? CompanyId { get; set; }

        [Required]
        public Guid CreatedByUserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}


