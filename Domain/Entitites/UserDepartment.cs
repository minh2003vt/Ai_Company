using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class UserDepartment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public Guid? DepartmentId { get; set; }
        public virtual User User { get; set; }
        public virtual Department? Department { get; set; }
        [Required]
        public Guid RoleId { get; set; }
        public virtual Role Role { get; set; }
    }
}
