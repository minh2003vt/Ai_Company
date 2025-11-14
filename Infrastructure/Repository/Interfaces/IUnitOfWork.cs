using System.Threading.Tasks;

namespace Infrastructure.Repository.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        ICompanyRepository Companies { get; }
        IDepartmentRepository Departments { get; }
        IRoleRepository Roles { get; }
        ILoginLogsRepository LoginLogs { get; }
        IActionLogRepository ActionLogs { get; }
        IUserDepartmentRepository UserDepartments { get; }
        IUserAiConfigRepository UserAiConfigs { get; }
        IAIConfigureRepository AIConfigures { get; }
        IChatSessionRepository ChatSessions { get; }
        IKnowledgeSourceRepository KnowledgeSources { get; }
        IAIConfigureVersionRepository AIConfigureVersions { get; }
        IAIConfigureCompanyRepository AIConfigureCompanies { get; }
        IAIConfigureCompanyDepartmentRepository AIConfigureCompanyDepartments { get; }
        IAIModelConfigRepository AIModelConfigs { get; }

        Task<int> SaveChangesAsync();
    }
}


