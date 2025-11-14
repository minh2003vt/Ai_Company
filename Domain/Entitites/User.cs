using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Domain.Entitites
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }   

        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        public DateTime DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } 
        // ===== Security & Login Tracking =====
        public DateTime? LastLoginAt { get; set; }         // lần login gần nhất
        public int FailedLoginAttempts { get; set; } = 0;  // số lần login sai liên tục
        public DateTime? BlockedUntil { get; set; }        // nếu block tạm thời
        public bool IsBlocked { get; set; } = false;       // trạng thái bị khóa


        // Company
        public Guid? CompanyId { get; set; }
        public virtual Company? Company { get; set; }

        public virtual ICollection<LoginLogs> LoginLog { get; set; } = new List<LoginLogs>();
        public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
        public virtual ICollection<UserAiConfig> UserAiConfigs { get; set; } = new List<UserAiConfig>();

    }
}
