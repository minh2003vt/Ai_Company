using Domain.Entitites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class ChatSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid AIConfigureId { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }

        public Guid UserId { get; set; } // owner of session
        public virtual User User { get; set; }

        public string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // For quick retrieval in Firestore/Realtime: store external id if needed
        public string ExternalSessionId { get; set; }

    }
}
