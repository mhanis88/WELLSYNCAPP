using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataSyncApp.Models
{
    [Table("Platforms")]
    public class Platform
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string UniqueName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        // Navigation property for related wells
        public virtual ICollection<Well> Wells { get; set; } = new List<Well>();

        // Timestamp for tracking database changes
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    }
}