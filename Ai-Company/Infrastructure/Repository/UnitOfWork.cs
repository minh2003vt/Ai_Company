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
        public IUserCompanyRepository UserCompanies { get; }
        public IUserDepartmentRepository UserDepartments { get; }
        public IAIConfigureRepository AIConfigures { get; }

        public UnitOfWork(
            AppDbContext dbContext,
            IUserRepository users,
            ICompanyRepository companies,
            IDepartmentRepository departments,
            IRoleRepository roles,
            ILoginLogsRepository loginLogs,
            IActionLogRepository actionLogs,
            IUserCompanyRepository userCompanies,
            IUserDepartmentRepository userDepartments,
            IAIConfigureRepository aiConfigures)
        {
            _dbContext = dbContext;
            Users = users;
            Companies = companies;
            Departments = departments;
            Roles = roles;
            LoginLogs = loginLogs;
            ActionLogs = actionLogs;
            UserCompanies = userCompanies;
            UserDepartments = userDepartments;
            AIConfigures = aiConfigures;
        }

        public Task<int> SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }
    }
}


