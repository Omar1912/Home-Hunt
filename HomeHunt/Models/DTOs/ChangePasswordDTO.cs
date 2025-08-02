using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.DTOs
{
    public class ChangePasswordDTO
    {
        public required string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$",
           ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one non-alphanumeric character.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "ConfirmPassword is required.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public required string ConfirmPassword { get; set;} = string.Empty;

    }
}
