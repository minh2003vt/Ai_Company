using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class AIConfigureRepository : GenericRepository<AI_Configure>, IAIConfigureRepository
    {
        public AIConfigureRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


