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
        IUserCompanyRepository UserCompanies { get; }
        IUserDepartmentRepository UserDepartments { get; }
        IAIConfigureRepository AIConfigures { get; }

        Task<int> SaveChangesAsync();
    }
}


