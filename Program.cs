using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DataSyncApp.Data;
using DataSyncApp.Services;

namespace DataSyncApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Setup dependency injection
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole())
                .AddDbContext<AppDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
                .AddSingleton<IConfiguration>(configuration)
                .AddScoped<ApiClient>()
                .AddScoped<DataSyncService>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting WellSync Data Sync Application...");

                // Ensure database is created
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Database.EnsureCreatedAsync();

                logger.LogInformation("Database connection verified.");

                // Get database stats before sync
                var statsBefore = await dbContext.GetDatabaseStatsAsync();
                logger.LogInformation("Database before sync: {PlatformCount} platforms, {WellCount} wells", 
                    statsBefore.PlatformCount, statsBefore.WellCount);

                // Perform data synchronization
                var syncService = scope.ServiceProvider.GetRequiredService<DataSyncService>();
                
                // Try with actual data first
                logger.LogInformation("Starting data synchronization with actual API data...");
                var syncResult = await syncService.SyncDataAsync(useDummyData: false);
                
                // If actual data fails, try with dummy data for testing flexible key handling
                if (!syncResult.Success)
                {
                    logger.LogWarning("Actual data sync failed: {Error}. Trying with dummy data for testing...", syncResult.ErrorMessage);
                    syncResult = await syncService.SyncDataAsync(useDummyData: true);
                }

                // Display sync results
                if (syncResult.Success)
                {
                    logger.LogInformation("Login successful. Token acquired.");
                    logger.LogInformation("Fetching platform and well data...");
                    logger.LogInformation("{PlatformTotal} platforms processed. {PlatformInserted} inserted, {PlatformUpdated} updated.",
                        syncResult.TotalPlatforms, syncResult.PlatformsInserted, syncResult.PlatformsUpdated);
                    logger.LogInformation("{WellTotal} wells processed. {WellInserted} inserted, {WellUpdated} updated.",
                        syncResult.TotalWells, syncResult.WellsInserted, syncResult.WellsUpdated);
                    logger.LogInformation("Sync completed successfully.");

                    // Get database stats after sync
                    var statsAfter = await syncService.GetDatabaseStatsAsync();
                    logger.LogInformation("Database after sync: {PlatformCount} platforms, {WellCount} wells", 
                        statsAfter.PlatformCount, statsAfter.WellCount);
                    
                    if (statsAfter.LastSyncTime != default)
                    {
                        logger.LogInformation("Last sync time: {LastSync:yyyy-MM-dd HH:mm:ss} UTC", statsAfter.LastSyncTime);
                    }
                }
                else
                {
                    logger.LogError("Sync failed: {Error}", syncResult.ErrorMessage);
                    logger.LogInformation("Please check API configuration and connectivity.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during application execution: {Message}", ex.Message);
            }
            finally
            {
                await serviceProvider.DisposeAsync();
            }
        }
    }
}