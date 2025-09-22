using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }
        public Guid CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
