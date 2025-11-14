using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites
{
    public class AI_ConfigureCompany
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid CompanyId { get; set; }
        public virtual Company Company { get; set; }

        [Required]
        public Guid AIConfigureId { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }

        // Created timestamp stored as UTC+7
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        // Optional note for extra context
        [MaxLength(255)]
        public string? Note { get; set; }
    }
}


