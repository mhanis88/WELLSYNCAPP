using System.ComponentModel.DataAnnotations;

namespace DataSyncApp.Models
{
    public class LoginRequest
    {
        [Required]
        [MinLength(5)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MinLength(5)]
        public string Password { get; set; } = string.Empty;
    }
}