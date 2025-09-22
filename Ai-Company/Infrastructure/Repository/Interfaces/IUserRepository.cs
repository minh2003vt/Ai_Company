using System;
using System.Threading.Tasks;
using Domain.Entitites;

namespace Infrastructure.Repository.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
    }
}


