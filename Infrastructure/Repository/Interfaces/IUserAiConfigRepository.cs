using Domain.Entitites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository.Interfaces
{
    public interface IUserAiConfigRepository : IGenericRepository<UserAiConfig>
    {
        Task<IEnumerable<UserAiConfig>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<UserAiConfig>> GetByAIConfigureIdAsync(Guid aiConfigureId);
        Task<UserAiConfig?> GetByUserAndAIAsync(Guid userId, Guid aiConfigureId);
        Task<bool> HasAccessAsync(Guid userId, Guid aiConfigureId);
    }
}
