using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataSyncApp.Models
{
    [Table("Wells")]
    public class Well
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlatformId { get; set; }

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

        // Foreign key relationship
        [ForeignKey("PlatformId")]
        public virtual Platform Platform { get; set; } = null!;

        // Timestamp for tracking database changes
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    }
}