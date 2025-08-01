using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataSyncApp.Data;
using DataSyncApp.Models;

namespace DataSyncApp.Services
{
    public class DataSyncService
    {
        private readonly AppDbContext _dbContext;
        private readonly ApiClient _apiClient;
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(AppDbContext dbContext, ApiClient apiClient, ILogger<DataSyncService> logger)
        {
            _dbContext = dbContext;
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Performs complete data synchronization from API to database
        /// </summary>
        public async Task<SyncResult> SyncDataAsync(bool useDummyData = false)
        {
            var result = new SyncResult();
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Starting data synchronization process...");

                // Step 1: Authenticate with API
                if (!await _apiClient.LoginAsync())
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to authenticate with API";
                    _logger.LogError("Sync failed: {Error}", result.ErrorMessage);
                    return result;
                }

                // Step 2: Fetch data from API
                List<PlatformDto>? platformData;
                if (useDummyData)
                {
                    _logger.LogInformation("Fetching dummy data for testing...");
                    platformData = await _apiClient.GetPlatformWellDummyAsync();
                }
                else
                {
                    _logger.LogInformation("Fetching actual platform/well data...");
                    platformData = await _apiClient.GetPlatformWellActualAsync();
                }

                if (platformData == null || !platformData.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No data received from API";
                    _logger.LogWarning("Sync completed with warning: {Warning}", result.ErrorMessage);
                    return result;
                }

                _logger.LogInformation("Received {PlatformCount} platforms with {WellCount} wells from API", 
                    platformData.Count, platformData.Sum(p => p.Well?.Count ?? 0));

                // Step 3: Begin database transaction
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // Step 4: Sync platforms
                    var platformResult = await SyncPlatformsAsync(platformData);
                    result.PlatformsInserted = platformResult.Inserted;
                    result.PlatformsUpdated = platformResult.Updated;

                    // Step 5: Sync wells
                    var wellResult = await SyncWellsAsync(platformData);
                    result.WellsInserted = wellResult.Inserted;
                    result.WellsUpdated = wellResult.Updated;

                    // Step 6: Commit transaction
                    await transaction.CommitAsync();
                    result.Success = true;

                    var endTime = DateTime.UtcNow;
                    result.Duration = endTime - startTime;

                    _logger.LogInformation("Sync completed successfully in {Duration:mm\\:ss}. " +
                        "Platforms: {PlatformInserted} inserted, {PlatformUpdated} updated. " +
                        "Wells: {WellInserted} inserted, {WellUpdated} updated.",
                        result.Duration,
                        result.PlatformsInserted, result.PlatformsUpdated,
                        result.WellsInserted, result.WellsUpdated);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Sync failed with exception: {Message}", ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Synchronize platform data with database
        /// </summary>
        private async Task<SyncEntityResult> SyncPlatformsAsync(List<PlatformDto> platformDtos)
        {
            var result = new SyncEntityResult();
            
            _logger.LogInformation("Synchronizing {Count} platforms...", platformDtos.Count);

            // Get existing platforms from database
            var existingPlatforms = await _dbContext.Platforms
                .ToDictionaryAsync(p => p.Id, p => p);

            foreach (var dto in platformDtos)
            {
                try
                {
                    if (existingPlatforms.TryGetValue(dto.Id, out var existingPlatform))
                    {
                        // Update existing platform
                        var hasChanges = UpdatePlatformFromDto(existingPlatform, dto);
                        if (hasChanges)
                        {
                            result.Updated++;
                            _logger.LogDebug("Updated platform ID {Id}: {Name}", dto.Id, dto.UniqueName);
                        }
                    }
                    else
                    {
                        // Insert new platform
                        var newPlatform = CreatePlatformFromDto(dto);
                        _dbContext.Platforms.Add(newPlatform);
                        result.Inserted++;
                        _logger.LogDebug("Inserted new platform ID {Id}: {Name}", dto.Id, dto.UniqueName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing platform ID {Id}: {Message}", dto.Id, ex.Message);
                    result.Errors++;
                }
            }

            // Save platform changes
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Platform sync completed: {Inserted} inserted, {Updated} updated, {Errors} errors",
                result.Inserted, result.Updated, result.Errors);

            return result;
        }

        /// <summary>
        /// Synchronize well data with database
        /// </summary>
        private async Task<SyncEntityResult> SyncWellsAsync(List<PlatformDto> platformDtos)
        {
            var result = new SyncEntityResult();
            
            // Flatten all wells from all platforms
            var allWells = platformDtos
                .Where(p => p.Well != null)
                .SelectMany(p => p.Well)
                .ToList();

            _logger.LogInformation("Synchronizing {Count} wells...", allWells.Count);

            if (!allWells.Any())
            {
                _logger.LogInformation("No wells to synchronize");
                return result;
            }

            // Get existing wells from database
            var existingWells = await _dbContext.Wells
                .ToDictionaryAsync(w => w.Id, w => w);

            foreach (var dto in allWells)
            {
                try
                {
                    if (existingWells.TryGetValue(dto.Id, out var existingWell))
                    {
                        // Update existing well
                        var hasChanges = UpdateWellFromDto(existingWell, dto);
                        if (hasChanges)
                        {
                            result.Updated++;
                            _logger.LogDebug("Updated well ID {Id}: {Name}", dto.Id, dto.UniqueName);
                        }
                    }
                    else
                    {
                        // Insert new well
                        var newWell = CreateWellFromDto(dto);
                        _dbContext.Wells.Add(newWell);
                        result.Inserted++;
                        _logger.LogDebug("Inserted new well ID {Id}: {Name}", dto.Id, dto.UniqueName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing well ID {Id}: {Message}", dto.Id, ex.Message);
                    result.Errors++;
                }
            }

            // Save well changes
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Well sync completed: {Inserted} inserted, {Updated} updated, {Errors} errors",
                result.Inserted, result.Updated, result.Errors);

            return result;
        }

        /// <summary>
        /// Create a new Platform entity from DTO
        /// </summary>
        private Platform CreatePlatformFromDto(PlatformDto dto)
        {
            return new Platform
            {
                Id = dto.Id,
                UniqueName = dto.UniqueName ?? $"Platform_{dto.Id}",
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                LastSyncedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Update existing Platform entity from DTO
        /// </summary>
        private bool UpdatePlatformFromDto(Platform platform, PlatformDto dto)
        {
            bool hasChanges = false;

            if (platform.UniqueName != dto.UniqueName)
            {
                platform.UniqueName = dto.UniqueName ?? $"Platform_{dto.Id}";
                hasChanges = true;
            }

            if (Math.Abs(platform.Latitude - dto.Latitude) > 0.000001)
            {
                platform.Latitude = dto.Latitude;
                hasChanges = true;
            }

            if (Math.Abs(platform.Longitude - dto.Longitude) > 0.000001)
            {
                platform.Longitude = dto.Longitude;
                hasChanges = true;
            }

            if (platform.CreatedAt != dto.CreatedAt)
            {
                platform.CreatedAt = dto.CreatedAt;
                hasChanges = true;
            }

            if (platform.UpdatedAt != dto.UpdatedAt)
            {
                platform.UpdatedAt = dto.UpdatedAt;
                hasChanges = true;
            }

            if (hasChanges)
            {
                platform.LastSyncedAt = DateTime.UtcNow;
            }

            return hasChanges;
        }

        /// <summary>
        /// Create a new Well entity from DTO
        /// </summary>
        private Well CreateWellFromDto(WellDto dto)
        {
            return new Well
            {
                Id = dto.Id,
                PlatformId = dto.PlatformId,
                UniqueName = dto.UniqueName ?? $"Well_{dto.Id}",
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                LastSyncedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Update existing Well entity from DTO
        /// </summary>
        private bool UpdateWellFromDto(Well well, WellDto dto)
        {
            bool hasChanges = false;

            if (well.PlatformId != dto.PlatformId)
            {
                well.PlatformId = dto.PlatformId;
                hasChanges = true;
            }

            if (well.UniqueName != dto.UniqueName)
            {
                well.UniqueName = dto.UniqueName ?? $"Well_{dto.Id}";
                hasChanges = true;
            }

            if (Math.Abs(well.Latitude - dto.Latitude) > 0.000001)
            {
                well.Latitude = dto.Latitude;
                hasChanges = true;
            }

            if (Math.Abs(well.Longitude - dto.Longitude) > 0.000001)
            {
                well.Longitude = dto.Longitude;
                hasChanges = true;
            }

            if (well.CreatedAt != dto.CreatedAt)
            {
                well.CreatedAt = dto.CreatedAt;
                hasChanges = true;
            }

            if (well.UpdatedAt != dto.UpdatedAt)
            {
                well.UpdatedAt = dto.UpdatedAt;
                hasChanges = true;
            }

            if (hasChanges)
            {
                well.LastSyncedAt = DateTime.UtcNow;
            }

            return hasChanges;
        }



        /// <summary>
        /// Get database statistics after sync
        /// </summary>
        public async Task<DatabaseStats> GetDatabaseStatsAsync()
        {
            return await _dbContext.GetDatabaseStatsAsync();
        }
    }

    /// <summary>
    /// Result of a complete sync operation
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int PlatformsInserted { get; set; }
        public int PlatformsUpdated { get; set; }
        public int WellsInserted { get; set; }
        public int WellsUpdated { get; set; }
        public TimeSpan Duration { get; set; }

        public int TotalPlatforms => PlatformsInserted + PlatformsUpdated;
        public int TotalWells => WellsInserted + WellsUpdated;
        public int TotalRecords => TotalPlatforms + TotalWells;
    }

    /// <summary>
    /// Result of syncing a specific entity type
    /// </summary>
    public class SyncEntityResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Errors { get; set; }

        public int Total => Inserted + Updated;
    }
}