using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataSyncApp.Models;

namespace DataSyncApp.Services
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiClient> _logger;
        private string? _bearerToken;
        private DateTime _tokenExpiry;

        public ApiClient(IConfiguration configuration, ILogger<ApiClient> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = new HttpClient();
            
            var baseUrl = _configuration["ApiSettings:BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
            
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Authenticate with the API and get a Bearer token
        /// </summary>
        public async Task<bool> LoginAsync()
        {
            try
            {
                _logger.LogInformation("Attempting to login to API...");

                var loginRequest = new LoginRequest
                {
                    Username = _configuration["ApiSettings:Username"] ?? "",
                    Password = _configuration["ApiSettings:Password"] ?? ""
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Account/Login", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Login response: {Response}", responseContent);

                    // Handle simple string token response (API returns just the token as a JSON string)
                    try
                    {
                        // The API returns a simple JSON string containing the token
                        // Example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                        var token = JsonSerializer.Deserialize<string>(responseContent);
                        
                        if (!string.IsNullOrEmpty(token))
                        {
                            _bearerToken = token;
                            _tokenExpiry = DateTime.UtcNow.AddHours(1); // Default 1 hour expiry
                            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
                            
                            _logger.LogInformation("Login successful. Token acquired");
                            return true;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse login response as string token: {Response}", responseContent);
                    }
                }

                _logger.LogError("Login failed. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during login: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Check if the current token is still valid
        /// </summary>
        public bool IsTokenValid()
        {
            return !string.IsNullOrEmpty(_bearerToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5);
        }

        /// <summary>
        /// Ensure we have a valid token, login if necessary
        /// </summary>
        private async Task<bool> EnsureValidTokenAsync()
        {
            if (IsTokenValid())
            {
                return true;
            }

            _logger.LogInformation("Token expired or invalid, attempting to re-authenticate...");
            return await LoginAsync();
        }

        /// <summary>
        /// Fetch platform and well data from GetPlatformWellActual endpoint
        /// </summary>
        public async Task<List<PlatformDto>?> GetPlatformWellActualAsync()
        {
            var endpoint = _configuration["ApiSettings:Endpoints:GetPlatformWellActual"] ?? "/api/PlatformWell/GetPlatformWellActual";
            return await GetPlatformWellDataAsync(endpoint, "actual");
        }

        /// <summary>
        /// Fetch platform and well data from GetPlatformWellDummy endpoint (for testing)
        /// </summary>
        public async Task<List<PlatformDto>?> GetPlatformWellDummyAsync()
        {
            var endpoint = _configuration["ApiSettings:Endpoints:GetPlatformWellDummy"] ?? "/api/PlatformWell/GetPlatformWellDummy";
            return await GetPlatformWellDataAsync(endpoint, "dummy");
        }

        /// <summary>
        /// Generic method to fetch platform and well data from any endpoint
        /// </summary>
        private async Task<List<PlatformDto>?> GetPlatformWellDataAsync(string endpoint, string dataType)
        {
            try
            {
                if (!await EnsureValidTokenAsync())
                {
                    _logger.LogError("Cannot fetch {DataType} data: Authentication failed", dataType);
                    return null;
                }

                _logger.LogInformation("Fetching {DataType} platform and well data from {Endpoint}...", dataType, endpoint);

                var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("{DataType} data response length: {Length} characters", dataType, responseContent.Length);

                    // Try different response formats
                    var platforms = await ParsePlatformDataAsync(responseContent, dataType);
                    
                    if (platforms != null)
                    {
                        _logger.LogInformation("Successfully fetched {PlatformCount} platforms with {WellCount} wells from {DataType} endpoint", 
                            platforms.Count, platforms.Sum(p => p.Well?.Count ?? 0), dataType);
                        return platforms;
                    }
                }
                else
                {
                    _logger.LogError("Failed to fetch {DataType} data. Status: {StatusCode}, Response: {Response}", 
                        dataType, response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching {DataType} data: {Message}", dataType, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Parse platform data from various response formats
        /// </summary>
        private Task<List<PlatformDto>?> ParsePlatformDataAsync(string responseContent, string dataType)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            try
            {
                // Try parsing as direct array of platforms
                var platforms = JsonSerializer.Deserialize<List<PlatformDto>>(responseContent, jsonOptions);
                if (platforms != null && platforms.Count > 0)
                {
                    _logger.LogDebug("Parsed {DataType} data as direct platform array", dataType);
                    return Task.FromResult<List<PlatformDto>?>(platforms);
                }
            }
            catch (JsonException)
            {
                // Continue to next parsing attempt
            }

            try
            {
                // Try parsing as wrapped response
                var apiResponse = JsonSerializer.Deserialize<PlatformWellResponse>(responseContent, jsonOptions);
                if (apiResponse?.Data != null && apiResponse.Data.Count > 0)
                {
                    _logger.LogDebug("Parsed {DataType} data as wrapped API response", dataType);
                    return Task.FromResult<List<PlatformDto>?>(apiResponse.Data);
                }
            }
            catch (JsonException)
            {
                // Continue to next parsing attempt
            }

            try
            {
                // Try parsing as generic object and extract data property
                using var document = JsonDocument.Parse(responseContent);
                if (document.RootElement.TryGetProperty("data", out var dataElement))
                {
                    var platforms = JsonSerializer.Deserialize<List<PlatformDto>>(dataElement.GetRawText(), jsonOptions);
                    if (platforms != null && platforms.Count > 0)
                    {
                        _logger.LogDebug("Parsed {DataType} data from 'data' property", dataType);
                        return Task.FromResult<List<PlatformDto>?>(platforms);
                    }
                }

                // Try parsing root element directly if it's an array
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var platforms = JsonSerializer.Deserialize<List<PlatformDto>>(responseContent, jsonOptions);
                    if (platforms != null)
                    {
                        _logger.LogDebug("Parsed {DataType} data as root array", dataType);
                        return Task.FromResult<List<PlatformDto>?>(platforms);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse {DataType} response as JSON: {Message}", dataType, ex.Message);
            }

            _logger.LogWarning("Could not parse {DataType} response in any known format. Response preview: {Preview}", 
                dataType, responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);
            
            return Task.FromResult<List<PlatformDto>?>(null);
        }

        /// <summary>
        /// Test API connectivity
        /// </summary>
        public async Task<bool> TestConnectivityAsync()
        {
            try
            {
                _logger.LogInformation("Testing API connectivity...");
                var response = await _httpClient.GetAsync("/api/health");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("API connectivity test successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("API connectivity test returned status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API connectivity test failed: {Message}", ex.Message);
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}