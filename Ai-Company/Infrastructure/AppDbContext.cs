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
        public DbSet<UserCompany> UserCompanies { get; set; }
        public DbSet<UserDepartment> UserDepartments { get; set; }
        public DbSet<LoginLogs> LoginLogs { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<AI_Configure> AI_Configurations { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<KnowledgeSource> KnowledgeSources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite keys
            modelBuilder.Entity<UserCompany>().HasKey(uc => new { uc.UserId, uc.CompanyId });
            modelBuilder.Entity<UserDepartment>().HasKey(ud => new { ud.UserId, ud.DepartmentId, ud.AIId });

            // User - Role (many-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Company - Department (one-to-many)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserCompany (many-to-many via join entity)
            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCompanies)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCompany>()
                .HasOne(uc => uc.Company)
                .WithMany(c => c.UserCompanies)
                .HasForeignKey(uc => uc.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserDepartment (payload join)
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
                .HasOne(ud => ud.AI_Configure)
                .WithMany()
                .HasForeignKey(ud => ud.AIId)
                .OnDelete(DeleteBehavior.Cascade);

            // AI_Configure -> CreatedBy (User)
            modelBuilder.Entity<AI_Configure>()
                .HasOne(a => a.CreatedBy)
                .WithMany()
                .HasForeignKey(a => a.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // User - AI_Configure (many-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.AIConfigure)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.AIConfigureId)
                .OnDelete(DeleteBehavior.SetNull);

            // User - Department (many-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
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

            // ChatMessage - ChatSession (many-to-one)
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.ChatSession)
                .WithMany(cs => cs.Messages)
                .HasForeignKey(cm => cm.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage - KnowledgeSource (many-to-one)
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.KnowledgeSource)
                .WithMany()
                .HasForeignKey(cm => cm.KnowledgeSourceId)
                .OnDelete(DeleteBehavior.NoAction);

            // KnowledgeSource - AI_Configure (many-to-one)
            modelBuilder.Entity<KnowledgeSource>()
                .HasOne(ks => ks.AI_Configure)
                .WithMany(a => a.KnowledgeSources)
                .HasForeignKey(ks => ks.AIConfigureId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore collections that would create implicit many-to-many separate from payload join
            modelBuilder.Entity<Department>().Ignore(d => d.Users);
            modelBuilder.Entity<AI_Configure>().Ignore(a => a.Users);
        }
    }
}
