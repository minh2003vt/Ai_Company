using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class UserDepartment
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid DepartmentId { get; set; }
        public virtual User User { get; set; }
        public virtual Department Department { get; set; }
        [Required]
        public Guid AIId { get; set; }
        [Required]
        public virtual AI_Configure AI_Configure { get; set; }

    }
}
