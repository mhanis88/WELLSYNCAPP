using Microsoft.EntityFrameworkCore;
using DataSyncApp.Models;

namespace DataSyncApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets for our entities
        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Well> Wells { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Platform entity
            modelBuilder.Entity<Platform>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedNever(); // Use API-provided IDs
                entity.Property(p => p.UniqueName).IsRequired().HasMaxLength(255);
                entity.Property(p => p.Latitude).IsRequired();
                entity.Property(p => p.Longitude).IsRequired();
                entity.Property(p => p.CreatedAt).IsRequired();
                entity.Property(p => p.UpdatedAt).IsRequired();
                entity.Property(p => p.LastSyncedAt).IsRequired();

                // Index for performance
                entity.HasIndex(p => p.UniqueName).IsUnique();
                entity.HasIndex(p => p.LastSyncedAt);
            });

            // Configure Well entity
            modelBuilder.Entity<Well>(entity =>
            {
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).ValueGeneratedNever(); // Use API-provided IDs
                entity.Property(w => w.PlatformId).IsRequired();
                entity.Property(w => w.UniqueName).IsRequired().HasMaxLength(255);
                entity.Property(w => w.Latitude).IsRequired();
                entity.Property(w => w.Longitude).IsRequired();
                entity.Property(w => w.CreatedAt).IsRequired();
                entity.Property(w => w.UpdatedAt).IsRequired();
                entity.Property(w => w.LastSyncedAt).IsRequired();

                // Configure foreign key relationship
                entity.HasOne(w => w.Platform)
                      .WithMany(p => p.Wells)
                      .HasForeignKey(w => w.PlatformId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Indexes for performance
                entity.HasIndex(w => w.UniqueName);
                entity.HasIndex(w => w.PlatformId);
                entity.HasIndex(w => w.LastSyncedAt);
            });

            // Seed data (optional - for testing)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // This method can be used to seed initial data if needed
            // For now, we'll leave it empty as data comes from API
        }

        // Override SaveChanges to automatically update LastSyncedAt
        public override int SaveChanges()
        {
            UpdateSyncTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateSyncTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateSyncTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Platform || e.Entity is Well)
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Platform platform)
                {
                    platform.LastSyncedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is Well well)
                {
                    well.LastSyncedAt = DateTime.UtcNow;
                }
            }
        }
    }
}