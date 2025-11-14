using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<UserDepartment> UserDepartments { get; set; }
        public DbSet<UserAiConfig> UserAiConfigs { get; set; }
        public DbSet<LoginLogs> LoginLogs { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<AIModelConfig> AIModelConfigs { get; set; }
        public DbSet<AI_Configure> AI_Configurations { get; set; }
        public DbSet<AI_ConfigureVersion> AI_ConfigurationVersions { get; set; }
        public DbSet<AI_ConfigureCompany> AI_ConfigurationCompanies { get; set; }
        public DbSet<AI_ConfigureCompanyDepartment> AI_ConfigurationCompanyDepartments { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<KnowledgeSource> KnowledgeSources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite keys
            // UserDepartment now uses Id as primary key (configured via [Key] attribute)
            modelBuilder.Entity<UserAiConfig>().HasKey(uac => new { uac.UserId, uac.AIConfigureId });


            // Company - Department (one-to-many)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserDepartment (many-to-many with role)
            modelBuilder.Entity<UserDepartment>()
                .HasOne(ud => ud.User)
                .WithMany()
                .HasForeignKey(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserDepartment>()
                .HasOne(ud => ud.Department)
                .WithMany()
                .HasForeignKey(ud => ud.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserDepartment>()
                .HasOne(ud => ud.Role)
                .WithMany()
                .HasForeignKey(ud => ud.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserAiConfig (many-to-many)
            modelBuilder.Entity<UserAiConfig>()
                .HasOne(uac => uac.User)
                .WithMany(u => u.UserAiConfigs)
                .HasForeignKey(uac => uac.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAiConfig>()
                .HasOne(uac => uac.AI_Configure)
                .WithMany()
                .HasForeignKey(uac => uac.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            // AI_Configure -> CreatedBy (User)
            modelBuilder.Entity<AI_Configure>()
                .HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // AI_Configure -> Company (optional)
            modelBuilder.Entity<AI_Configure>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // AI_Configure -> AIModelConfig (many-to-one: multiple AI_Configure can share one config)
            modelBuilder.Entity<AI_Configure>()
                .HasOne(a => a.ModelConfig)
                .WithMany(m => m.AI_Configures)
                .HasForeignKey(a => a.ModelConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            // AI_ConfigureVersion relationships
            modelBuilder.Entity<AI_ConfigureVersion>()
                .HasOne(v => v.AI_Configure)
                .WithMany()
                .HasForeignKey(v => v.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AI_ConfigureVersion>()
                .HasIndex(v => new { v.AIConfigureId, v.Version })
                .IsUnique();

            // User -> LoginLogs (one-to-many)
            modelBuilder.Entity<LoginLogs>()
                .HasOne(l => l.User)
                .WithMany(u => u.LoginLog)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // LoginLogs -> ActionLog (one-to-many)
            modelBuilder.Entity<ActionLog>()
                .HasOne(a => a.LoginLog)
                .WithMany(l => l.Actions)
                .HasForeignKey(a => a.LoginLogId)
                .OnDelete(DeleteBehavior.Cascade);


            // User - Company (many-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            // ChatSession - AI_Configure (many-to-one)
            modelBuilder.Entity<ChatSession>()
                .HasOne(cs => cs.AI_Configure)
                .WithMany(a => a.ChatSessions)
                .HasForeignKey(cs => cs.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatSession - User (many-to-one)
            modelBuilder.Entity<ChatSession>()
                .HasOne(cs => cs.User)
                .WithMany(u => u.ChatSessions)
                .HasForeignKey(cs => cs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // KnowledgeSource - AI_Configure (many-to-one)
            modelBuilder.Entity<KnowledgeSource>()
                .HasOne(ks => ks.AI_Configure)
                .WithMany(a => a.KnowledgeSources)
                .HasForeignKey(ks => ks.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            // No implicit many-to-many ignores needed now

            // AI_ConfigureCompany link (Global AI share to companies)
            modelBuilder.Entity<AI_ConfigureCompany>()
                .HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AI_ConfigureCompany>()
                .HasOne(x => x.AI_Configure)
                .WithMany()
                .HasForeignKey(x => x.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AI_ConfigureCompany>()
                .HasIndex(x => new { x.CompanyId, x.AIConfigureId })
                .IsUnique();

            // AI_ConfigureCompanyDepartment link
            modelBuilder.Entity<AI_ConfigureCompanyDepartment>()
                .HasOne(x => x.AI_ConfigureCompany)
                .WithMany()
                .HasForeignKey(x => x.AIConfigureCompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AI_ConfigureCompanyDepartment>()
                .HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AI_ConfigureCompanyDepartment>()
                .HasIndex(x => new { x.AIConfigureCompanyId, x.DepartmentId })
                .IsUnique();
        }
    }
}
