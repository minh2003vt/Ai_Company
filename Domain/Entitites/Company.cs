using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entitites.Enums;

namespace Domain.Entitites
{
    public class Company
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } 
        [Required]
        [MaxLength(100)]
        public string CompanyCode { get; set; }

        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Description { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        public int MaximumUser { get; set; } = 10;

        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.OneMonth;

        public DateTime StartSubscriptionDate { get; set; } = DateTime.UtcNow;

        public CompanyStatus Status { get; set; } = CompanyStatus.Active;

        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
