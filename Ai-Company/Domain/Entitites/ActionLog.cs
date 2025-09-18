using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class ActionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid LoginLogId { get; set; }
        public virtual LoginLogs LoginLog { get; set; }
        public DateTime ActionTime { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string ActionType { get; set; } // "ViewProfile", "CreateOrder", "UpdateInfo", etc.

        [MaxLength(500)]
        public string ActionDetail { get; set; } // e.g. "User updated email"

        [MaxLength(200)]
        public string Endpoint { get; set; } // API endpoint 

        [MaxLength(45)]
        public string IpAddress { get; set; }

    }
}
