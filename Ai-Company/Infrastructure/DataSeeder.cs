using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext dbContext)
        {
            await dbContext.Database.MigrateAsync();

            // Seed Roles
            var adminRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = "Admin",
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Roles.Add(adminRole);
                await dbContext.SaveChangesAsync();
            }

            // Seed Admin User
            var adminEmail = "admin@company.local";
            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    FullName = "System Administrator",
                    Email = adminEmail,
                    // Mật khẩu mặc định: admin123!
                    PasswordHash = Infrastructure.Security.PasswordHasher.HashPassword("admin123!"), 
                    PhoneNumber = "0000000000",
                    DateOfBirth = new DateTime(1990, 1, 1),
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(adminUser);
                await dbContext.SaveChangesAsync();
            }

            // Seed Company
            var companyTin = "0000000000";
            var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.TIN == companyTin);
            if (company == null)
            {
                company = new Company
                {
                    CompanyName = "Default Company",
                    TIN = companyTin,
                    Description = "Seeded default company"
                };
                dbContext.Companies.Add(company);
                await dbContext.SaveChangesAsync();
            }

            // Seed Department
            var departmentName = "General";
            var department = await dbContext.Departments.FirstOrDefaultAsync(d => d.Name == departmentName && d.CompanyId == company.Id);
            if (department == null)
            {
                department = new Department
                {
                    Name = departmentName,
                    CompanyId = company.Id
                };
                dbContext.Departments.Add(department);
                await dbContext.SaveChangesAsync();
            }

            // Map User to Company (UserCompany)
            var hasUserCompany = await dbContext.UserCompanies.AnyAsync(uc => uc.UserId == adminUser.Id && uc.CompanyId == company.Id);
            if (!hasUserCompany)
            {
                dbContext.UserCompanies.Add(new UserCompany
                {
                    UserId = adminUser.Id,
                    CompanyId = company.Id
                });
                await dbContext.SaveChangesAsync();
            }

            // Không seed UserDepartment vì cần AI_Configure (chưa có cấu hình mặc định)
        }
    }
}


