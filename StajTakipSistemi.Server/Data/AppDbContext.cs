using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<StudentUser> StudentUsers => Set<StudentUser>();
        public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
        public DbSet<StudentDatasetRecord> StudentDatasetRecords => Set<StudentDatasetRecord>();
        public DbSet<InternshipDocument> InternshipDocuments => Set<InternshipDocument>();
        public DbSet<FailedLoginLog> FailedLoginLogs => Set<FailedLoginLog>();
        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // NYP - KALITIM - Table Per Hierarchy Eşlemesi
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<StudentUser>("STUDENT")
                .HasValue<AdminUser>("ADMIN");

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            modelBuilder.Entity<StudentDatasetRecord>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.StudentNumber);
                entity.HasIndex(s => s.TcNo);
            });

            modelBuilder.Entity<InternshipDocument>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.StudentUserId);
                entity.HasIndex(d => d.Status);
            });
        }
    }
}
