using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        [MaxLength(1000)]
        public string Rule { get; set; }  // ví dụ: "Không chửi thề, không đưa thông tin mật"

        // Knowledge source: file path, vector db id, hoặc link
        public string KnowledgeSource { get; set; }
        public Guid CreatedByUserId { get; set; }
        public User CreatedBy { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
