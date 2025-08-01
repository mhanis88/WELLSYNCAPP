using System.Text.Json.Serialization;

namespace DataSyncApp.Models
{
    public class WellDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("platformId")]
        public int PlatformId { get; set; }

        [JsonPropertyName("uniqueName")]
        public string UniqueName { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}