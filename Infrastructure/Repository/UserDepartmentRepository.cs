using Domain.Entitites;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class UserDepartmentRepository : GenericRepository<UserDepartment>, IUserDepartmentRepository
    {
        public UserDepartmentRepository(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}


