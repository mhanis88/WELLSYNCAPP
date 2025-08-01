using System.Text.Json.Serialization;

namespace DataSyncApp.Models
{
    public class PlatformDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("uniqueName")]
        public string UniqueName { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        // Map the API's "lastUpdate" to CreatedAt (since CreatedAt is missing)
        [JsonPropertyName("lastUpdate")]
        public DateTime CreatedAt { get; set; }

        // Use the same lastUpdate for UpdatedAt
        [JsonIgnore]
        public DateTime UpdatedAt => CreatedAt;

        [JsonPropertyName("well")]
        public List<WellDto> Well { get; set; } = new List<WellDto>();
    }
}