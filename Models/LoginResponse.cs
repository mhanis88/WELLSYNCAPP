using System.Text.Json.Serialization;

namespace DataSyncApp.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expires")]
        public DateTime Expires { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}