using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.DTOs
{
    public class EmailVerificationDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public required string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Token is required.")]
        public required string Token { get; set; }

        [Required]
        public required string Code { get; set; }
    }
}