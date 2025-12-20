using Microsoft.EntityFrameworkCore;
using az_backend_new.Models;

namespace az_backend_new.Data
{
    public class AzDbContext : DbContext
    {
        public AzDbContext(DbContextOptions<AzDbContext> options) : base(options)
        {
        }

        public DbSet<Trainee> Trainees { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Trainee configuration
            modelBuilder.Entity<Trainee>(entity =>
            {
                entity.ToTable("Trainees");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SerialNumber).IsUnique();
                entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PersonName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.StreetAddress).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Certificate configuration
            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.ToTable("Certificates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // منع تكرار نفس الطريقة لنفس المتدرب
                entity.HasIndex(e => new { e.TraineeId, e.ServiceMethod }).IsUnique();

                // علاقة many-to-one مع المتدرب
                entity.HasOne(c => c.Trainee)
                    .WithMany(t => t.Certificates)
                    .HasForeignKey(c => c.TraineeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@azinternational.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = Role.Admin,
                    CreatedAt = new DateTime(2024, 12, 18, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 12, 18, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
