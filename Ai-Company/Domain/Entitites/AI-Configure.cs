using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string RulesJson { get; set; } 

        // Model settings for Gemini
        [MaxLength(200)]
        public string ModelName { get; set; } = "gemini-2.5-flash";

        public float Temperature { get; set; } = 0.0f;
        public int MaxOutputTokens { get; set; } = 512;
        public bool UseStreaming { get; set; } = false;

        // RAG settings: whether to use RAG and how many docs to retrieve
        public bool UseRag { get; set; } = true;
        public int RagTopK { get; set; } = 5;

        // Reference to knowledge sources (vector DBs, file sources, urls)
        public virtual ICollection<KnowledgeSource> KnowledgeSources { get; set; } = new List<KnowledgeSource>();

        // Which user created it (FK)
        public Guid CreatedByUserId { get; set; }
        public virtual User CreatedBy { get; set; }

        // optional list of users allowed to use this configuration
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    }




}
