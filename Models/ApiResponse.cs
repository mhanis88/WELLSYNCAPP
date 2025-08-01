using System.Text.Json.Serialization;

namespace DataSyncApp.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();
    }

    // Specific response for platform/well data
    public class PlatformWellResponse : ApiResponse<List<PlatformDto>>
    {
    }
}