using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entitites
{
    public class AI_ConfigureCompanyDepartment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid AIConfigureCompanyId { get; set; }
        public virtual AI_ConfigureCompany AI_ConfigureCompany { get; set; }

        [Required]
        public Guid DepartmentId { get; set; }
        public virtual Department Department { get; set; }
    }
}


