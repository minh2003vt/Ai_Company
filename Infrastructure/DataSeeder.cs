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
            var roles = new[] { "SystemAdmin", "Admin", "Manager", "Staff" };
            var roleEntities = new List<Role>();
            
            foreach (var roleName in roles)
            {
                var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null)
                {
                    role = new Role
                    {
                        Name = roleName,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Roles.Add(role);
                    await dbContext.SaveChangesAsync();
                }
                roleEntities.Add(role);
            }
            
            var systemAdminRole = roleEntities.First(r => r.Name == "SystemAdmin");
            var adminRole = roleEntities.First(r => r.Name == "Admin");

            // Seed Company first
            var companyName = "Default Company";
            var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.CompanyName == companyName);
            if (company == null)
            {
                company = new Company
                {
                    CompanyName = companyName,
                    CompanyCode = "DEFAULT001",
                    Description = "Seeded default company",
                    Website = "https://default-company.com",
                    MaximumUser = 50,
                    SubscriptionPlan = Domain.Entitites.Enums.SubscriptionPlan.OneYear,
                    StartSubscriptionDate = DateTime.UtcNow,
                    Status = Domain.Entitites.Enums.CompanyStatus.Active
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
                    Description = "General department for all users",
                    CompanyId = company.Id,
                };
                dbContext.Departments.Add(department);
                await dbContext.SaveChangesAsync();
            }

            // Seed System Admin User
            var systemAdminEmail = "systemadmin@company.local";
            var systemAdminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == systemAdminEmail);
            if (systemAdminUser == null)
            {
                systemAdminUser = new User
                {
                    FullName = "System Administrator",
                    Email = systemAdminEmail,
                    // Mật khẩu mặc định: systemadmin123!
                    PasswordHash = Infrastructure.Security.PasswordHasher.HashPassword("systemadmin123!"), 
                    PhoneNumber = "0000000000",
                    DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 1, 1), DateTimeKind.Utc),
                    CompanyId = company.Id, // Gán CompanyId ngay từ đầu
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(systemAdminUser);
                await dbContext.SaveChangesAsync();
            }

            // Seed Admin User
            var adminEmail = "admin@company.local";
            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    FullName = "Company Administrator",
                    Email = adminEmail,
                    // Mật khẩu mặc định: admin123!
                    PasswordHash = Infrastructure.Security.PasswordHasher.HashPassword("admin123!"), 
                    PhoneNumber = "0000000001",
                    DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 1, 1), DateTimeKind.Utc),
                    CompanyId = company.Id, // Gán CompanyId ngay từ đầu
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Users.Add(adminUser);
                await dbContext.SaveChangesAsync();
            }

            // Create UserDepartment for SystemAdmin (DepartmentId = null)
            var systemAdminUserDept = await dbContext.UserDepartments
                .FirstOrDefaultAsync(ud => ud.UserId == systemAdminUser.Id && ud.RoleId == systemAdminRole.Id);
            if (systemAdminUserDept == null)
            {
                systemAdminUserDept = new UserDepartment
                {
                    UserId = systemAdminUser.Id,
                    DepartmentId = null, // SystemAdmin không thuộc department nào
                    RoleId = systemAdminRole.Id
                };
                dbContext.UserDepartments.Add(systemAdminUserDept);
            }

            // Create UserDepartment for Admin (DepartmentId = null)
            var adminUserDept = await dbContext.UserDepartments
                .FirstOrDefaultAsync(ud => ud.UserId == adminUser.Id && ud.RoleId == adminRole.Id);
            if (adminUserDept == null)
            {
                adminUserDept = new UserDepartment
                {
                    UserId = adminUser.Id,
                    DepartmentId = null, // Admin không thuộc department nào
                    RoleId = adminRole.Id
                };
                dbContext.UserDepartments.Add(adminUserDept);
            }

            await dbContext.SaveChangesAsync();

            // Seed AIModelConfig
            // Đảm bảo chỉ có một model active tại một thời điểm
            var existingActiveModel = await dbContext.AIModelConfigs.FirstOrDefaultAsync(m => m.Active);
            var defaultModelId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            // Xử lý model mẫu với ID cố định
            var existingModelWithId = await dbContext.AIModelConfigs.FirstOrDefaultAsync(m => m.Id == defaultModelId);
            var existingModelWithName = await dbContext.AIModelConfigs.FirstOrDefaultAsync(m => m.ModelName == "gemini-2.5-pro");
            
            if (existingModelWithId == null)
            {
                // Nếu chưa có model với ID cố định
                if (existingModelWithName != null && existingModelWithName.Id != defaultModelId)
                {
                    // Nếu có model với tên đó nhưng ID khác
                    // Cập nhật tất cả AI_Configure đang dùng model cũ sang model mới
                    var aiConfiguresUsingOldModel = await dbContext.AI_Configurations
                        .Where(a => a.ModelConfigId == existingModelWithName.Id)
                        .ToListAsync();
                    
                    // Tạo model mới với ID cố định trước
                    var defaultModel = new AIModelConfig
                    {
                        Id = defaultModelId,
                        ModelName = "gemini-2.5-pro",
                        Temperature = existingModelWithName.Temperature,
                        MaxOutputTokens = existingModelWithName.MaxOutputTokens,
                        UseStreaming = existingModelWithName.UseStreaming,
                        ApiKey = existingModelWithName.ApiKey,
                        TopP = existingModelWithName.TopP,
                        TopK = existingModelWithName.TopK,
                        Active = existingModelWithName.Active,
                        CreatedAt = existingModelWithName.CreatedAt
                    };
                    dbContext.AIModelConfigs.Add(defaultModel);
                    await dbContext.SaveChangesAsync();
                    
                    // Cập nhật foreign keys
                    foreach (var aiConfig in aiConfiguresUsingOldModel)
                    {
                        aiConfig.ModelConfigId = defaultModelId;
                    }
                    await dbContext.SaveChangesAsync();
                    
                    // Xóa model cũ
                    dbContext.AIModelConfigs.Remove(existingModelWithName);
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    // Tạo model mới với ID cố định
                    var defaultModel = new AIModelConfig
                    {
                        Id = defaultModelId,
                        ModelName = "gemini-2.5-pro",
                        Temperature = 0.85f,
                        MaxOutputTokens = 8192,
                        UseStreaming = false,
                        ApiKey = null, // Sẽ fallback về config từ appsettings
                        TopP = 0.95f,
                        TopK = 40,
                        Active = existingActiveModel == null, // Active nếu chưa có model nào active
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };
                    dbContext.AIModelConfigs.Add(defaultModel);
                    await dbContext.SaveChangesAsync();
                }
            }
            
            // Tạo các model config mặc định khác
            var defaultModels = new[]
            {
                new AIModelConfig
                {
                    ModelName = "gemini-2.5-flash",
                    Temperature = 0.7f,
                    MaxOutputTokens = 8192,
                    UseStreaming = false,
                    ApiKey = null,
                    TopP = 0.9f,
                    TopK = 32,
                    Active = false,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                },
                new AIModelConfig
                {
                    ModelName = "gemini-1.5-pro",
                    Temperature = 0.9f,
                    MaxOutputTokens = 8192,
                    UseStreaming = false,
                    ApiKey = null,
                    TopP = 0.95f,
                    TopK = 40,
                    Active = false,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
                }
            };

            foreach (var model in defaultModels)
            {
                var existingModel = await dbContext.AIModelConfigs.FirstOrDefaultAsync(m => m.ModelName == model.ModelName);
                
                if (existingModel == null)
                {
                    dbContext.AIModelConfigs.Add(model);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}


