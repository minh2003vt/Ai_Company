using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class AIConfigureVersionRepository : GenericRepository<AI_ConfigureVersion>, IAIConfigureVersionRepository
    {
        public AIConfigureVersionRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


