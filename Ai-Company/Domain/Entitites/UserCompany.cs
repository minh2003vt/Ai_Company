using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class UserCompany
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public Guid CompanyId { get; set; }
        public virtual User User { get; set; } 
        public virtual Company Company { get; set; } 

    }
}
