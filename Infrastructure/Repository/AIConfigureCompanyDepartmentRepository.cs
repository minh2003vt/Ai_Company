using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class AIConfigureCompanyDepartmentRepository : GenericRepository<AI_ConfigureCompanyDepartment>, IAIConfigureCompanyDepartmentRepository
    {
        public AIConfigureCompanyDepartmentRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


