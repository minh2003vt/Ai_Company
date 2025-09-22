using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class LoginLogsRepository : GenericRepository<LoginLogs>, ILoginLogsRepository
    {
        public LoginLogsRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


