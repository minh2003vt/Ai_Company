using Domain.Entitites;
using Infrastructure.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class UserAiConfigRepository : GenericRepository<UserAiConfig>, IUserAiConfigRepository
    {
        private readonly AppDbContext _dbContext;

        public UserAiConfigRepository(AppDbContext context) : base(context)
        {
            _dbContext = context;
        }

        public async Task<IEnumerable<UserAiConfig>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.UserAiConfigs
                .Where(uac => uac.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserAiConfig>> GetByAIConfigureIdAsync(Guid aiConfigureId)
        {
            return await _dbContext.UserAiConfigs
                .Where(uac => uac.AIConfigureId == aiConfigureId)
                .ToListAsync();
        }

        public async Task<UserAiConfig?> GetByUserAndAIAsync(Guid userId, Guid aiConfigureId)
        {
            return await _dbContext.UserAiConfigs
                .FirstOrDefaultAsync(uac => uac.UserId == userId && uac.AIConfigureId == aiConfigureId);
        }

        public async Task<bool> HasAccessAsync(Guid userId, Guid aiConfigureId)
        {
            return await _dbContext.UserAiConfigs
                .AnyAsync(uac => uac.UserId == userId && uac.AIConfigureId == aiConfigureId);
        }
    }
}
