using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


