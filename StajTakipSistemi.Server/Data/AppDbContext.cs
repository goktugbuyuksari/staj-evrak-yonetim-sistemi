using Microsoft.EntityFrameworkCore;
using StajTakipSistemi.Server.Models.Domain;

namespace StajTakipSistemi.Server.Data
{
    // VERİTABANI: Entity Framework Core Code-First veritabanı yönetim sınıfı
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // VERİTABANI: Uygulamada kullanılan ilişkisel tablolar (DbSet)
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

            // OOP & DB: Table Per Hierarchy (TPH) kalıtım eşlemesi (Öğrenci ve Admin tek Users tablosunda UserType kolonuyla saklanır)
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<StudentUser>("STUDENT")
                .HasValue<AdminUser>("ADMIN");

            // VERİTABANI: Users tablosu birincil anahtar ve benzersiz (unique) indeksleri
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // VERİTABANI: Öğrenci veri seti arama indeksleri (Hızlı sorgulama için)
            modelBuilder.Entity<StudentDatasetRecord>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.StudentNumber);
                entity.HasIndex(s => s.TcNo);
            });

            // VERİTABANI: Ek-16 staj belgeleri tablosu indeksleri (Öğrenci Id ve Durum)
            modelBuilder.Entity<InternshipDocument>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.StudentUserId);
                entity.HasIndex(d => d.Status);
            });
        }
    }
}
