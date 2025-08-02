using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.Entities
{
    public class RegistrationDto
    {
        [Required (ErrorMessage = "Email is required.")]
        [EmailAddress (ErrorMessage = "Invalid email address format.")]
        [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;
        

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one non-alphanumeric character.")]
        public string Password { get; set; } = string.Empty; 


        [Required(ErrorMessage = "ConfirmPassword is required.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(20,ErrorMessage = "First Name cannot exceed 20 characters.")]
        [MinLength(3, ErrorMessage = "First Name must be at least 2 characters long.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(20, ErrorMessage = "Last Name cannot exceed 20 characters.")]
        [MinLength(3, ErrorMessage = "Last Name must be at least 2 characters long.")]
        public string LastName { get; set; } = string.Empty;


        [MaxLength(30, ErrorMessage = "Mobile number cannot exceed 30 characters.")]
        [RegularExpression(@"^\+?[\d- ]+$", ErrorMessage = "Mobile number must contain only digits, dashes, or a plus sign.")]
        public string? MobileNumber { get; set; }


    }
}