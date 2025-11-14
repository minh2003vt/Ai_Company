using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class AIModelConfigRepository : GenericRepository<AIModelConfig>, IAIModelConfigRepository
    {
        public AIModelConfigRepository(AppDbContext context) : base(context)
        {
        }
    }
}

