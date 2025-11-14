using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class UserAiConfig
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid AIConfigureId { get; set; }
        public virtual User User { get; set; }
        public virtual AI_Configure AI_Configure { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
