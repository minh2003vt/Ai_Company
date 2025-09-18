using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; }

        [Required]
        [MaxLength(15)]
        public string TIN { get; set; }

        [Required]
        [MaxLength(100)]
        public string Description { get; set; }
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
    }
}
