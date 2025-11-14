using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class KnowledgeSourceRepository : GenericRepository<KnowledgeSource>, IKnowledgeSourceRepository
    {
        public KnowledgeSourceRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


