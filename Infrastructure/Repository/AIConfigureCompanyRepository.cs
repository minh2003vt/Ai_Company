using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class AIConfigureCompanyRepository : GenericRepository<AI_ConfigureCompany>, IAIConfigureCompanyRepository
    {
        public AIConfigureCompanyRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


