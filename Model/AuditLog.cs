using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Model
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Action { get; set; } // e.g., "Login", "Logout", "Password Changed"

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string IPAddress { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } // Link to User
    }
}
