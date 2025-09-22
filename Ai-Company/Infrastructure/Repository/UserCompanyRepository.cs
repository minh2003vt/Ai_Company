using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class UserCompanyRepository : GenericRepository<UserCompany>, IUserCompanyRepository
    {
        public UserCompanyRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


