using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entitites.Enums;

namespace Domain.Entitites
{
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ChatSessionId { get; set; }
        public virtual ChatSession ChatSession { get; set; }

        public MessageRole Role { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: pointer to which knowledge source produced this content (for provenance)
        public Guid? KnowledgeSourceId { get; set; }
        public virtual KnowledgeSource KnowledgeSource { get; set; }

        // Optional: store model output metadata (tokens, score)
        public string MetaJson { get; set; }
    }
}
