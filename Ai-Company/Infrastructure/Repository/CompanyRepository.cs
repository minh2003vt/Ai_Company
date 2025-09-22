using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {
        public CompanyRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


