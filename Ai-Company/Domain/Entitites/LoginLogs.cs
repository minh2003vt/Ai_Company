using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class LoginLogs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        [MaxLength(45)] 
        public string IpAddress { get; set; }

        [MaxLength(200)]
        public string Device { get; set; }  

        [MaxLength(200)]
        public string Location { get; set; } 

        [MaxLength(50)]
        public string LoginMethod { get; set; }
        public ICollection<ActionLog> Actions { get; set; } = new List<ActionLog>();


    }
}
