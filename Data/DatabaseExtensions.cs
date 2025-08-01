using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSyncApp.Data
{
    public static class DatabaseExtensions
    {
        public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

            try
            {
                logger.LogInformation("Initializing database...");

                // Ensure database is created
                await context.Database.EnsureCreatedAsync();

                // Check if migrations are needed (for future use)
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying pending migrations: {Migrations}", 
                        string.Join(", ", pendingMigrations));
                    await context.Database.MigrateAsync();
                }

                logger.LogInformation("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }

        public static async Task<DatabaseStats> GetDatabaseStatsAsync(this AppDbContext context)
        {
            var platformCount = await context.Platforms.CountAsync();
            var wellCount = await context.Wells.CountAsync();
            
            DateTime lastSyncTime = default;
            if (platformCount > 0 || wellCount > 0)
            {
                var syncTimes = await context.Platforms
                    .Select(p => p.LastSyncedAt)
                    .Union(context.Wells.Select(w => w.LastSyncedAt))
                    .ToListAsync();
                
                if (syncTimes.Any())
                {
                    lastSyncTime = syncTimes.Max();
                }
            }

            return new DatabaseStats
            {
                PlatformCount = platformCount,
                WellCount = wellCount,
                LastSyncTime = lastSyncTime
            };
        }
    }

    public class DatabaseStats
    {
        public int PlatformCount { get; set; }
        public int WellCount { get; set; }
        public DateTime LastSyncTime { get; set; }
    }
}