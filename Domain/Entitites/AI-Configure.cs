using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Entitites.Enums;

namespace Domain.Entitites
{
    public class AI_Configure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(255)]
        public string Description { get; set; }

        // Simple rule text or system instruction for model
        [Required, MaxLength(2000)]
        public string Rules { get; set; } 

        // Model configuration (ModelName, Temperature, MaxOutputTokens, ApiKey, etc.)
        [Required]
        public Guid ModelConfigId { get; set; }
        public virtual AIModelConfig ModelConfig { get; set; }

        // RAG settings: whether to use RAG and how many docs to retrieve
        public int RagTopK { get; set; } = 5;

        // Versioning - current live version name
        [MaxLength(50)]
        public string? CurrentVersion { get; set; }

        // Kind: Global (SystemAdmin) or Company
        [Required]
        public AI_ConfigureKind Kind { get; set; }

        // CompanyId: null for Global, required for Company
        public Guid? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        // Reference to knowledge sources (vector DBs, file sources, urls)
        public virtual ICollection<KnowledgeSource> KnowledgeSources { get; set; } = new List<KnowledgeSource>();

        // Which user created it (FK)
        public Guid CreatedByUserId { get; set; }
        public virtual User CreatedBy { get; set; }

        // optional list of users allowed to use this configuration
        public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
        public virtual ICollection<UserAiConfig> UserAiConfigs { get; set; } = new List<UserAiConfig>();
    }




}
