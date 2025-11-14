using System.Threading.Tasks;
using Infrastructure.Repository.Interfaces;

namespace Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;

        public IUserRepository Users { get; }
        public ICompanyRepository Companies { get; }
        public IDepartmentRepository Departments { get; }
        public IRoleRepository Roles { get; }
        public ILoginLogsRepository LoginLogs { get; }
        public IActionLogRepository ActionLogs { get; }
        public IUserDepartmentRepository UserDepartments { get; }
        public IUserAiConfigRepository UserAiConfigs { get; }
        public IAIConfigureRepository AIConfigures { get; }
        public IChatSessionRepository ChatSessions { get; }
        public IKnowledgeSourceRepository KnowledgeSources { get; }
        public IAIConfigureVersionRepository AIConfigureVersions { get; }
        public IAIConfigureCompanyRepository AIConfigureCompanies { get; }
        public IAIConfigureCompanyDepartmentRepository AIConfigureCompanyDepartments { get; }
        public IAIModelConfigRepository AIModelConfigs { get; }

        public UnitOfWork(
            AppDbContext dbContext,
            IUserRepository users,
            ICompanyRepository companies,
            IDepartmentRepository departments,
            IRoleRepository roles,
            ILoginLogsRepository loginLogs,
            IActionLogRepository actionLogs,
            IUserDepartmentRepository userDepartments,
            IUserAiConfigRepository userAiConfigs,
            IAIConfigureRepository aiConfigures,
            IChatSessionRepository chatSessions,
            IKnowledgeSourceRepository knowledgeSources,
            IAIConfigureVersionRepository aiConfigureVersions,
            IAIConfigureCompanyRepository aiConfigureCompanies,
            IAIConfigureCompanyDepartmentRepository aiConfigureCompanyDepartments,
            IAIModelConfigRepository aiModelConfigs
            )
        {
            _dbContext = dbContext;
            Users = users;
            Companies = companies;
            Departments = departments;
            Roles = roles;
            LoginLogs = loginLogs;
            ActionLogs = actionLogs;
            UserDepartments = userDepartments;
            UserAiConfigs = userAiConfigs;
            AIConfigures = aiConfigures;
            ChatSessions = chatSessions;
            KnowledgeSources = knowledgeSources;
            AIConfigureVersions = aiConfigureVersions;
            AIConfigureCompanies = aiConfigureCompanies;
            AIConfigureCompanyDepartments = aiConfigureCompanyDepartments;
            AIModelConfigs = aiModelConfigs;
        }

        public Task<int> SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}


