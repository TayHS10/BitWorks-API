using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GPP_API.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; } // Primary Key for this table

        [Required]
        public int UserId { get; set; } // Foreign key to the User table

        [Required]
        [StringLength(255)] // Store the actual token string (hashed or plain, but usually plain here for comparison)
        public string Token { get; set; } = null!;

        [Required]
        public DateTime ExpiresAt { get; set; } // When the token becomes invalid

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // When the token was generated

        public DateTime? UsedAt { get; set; } // Timestamp when the token was successfully used (optional, for auditing)

        // Navigation property to the User
        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // Link back to the user
    }
}
