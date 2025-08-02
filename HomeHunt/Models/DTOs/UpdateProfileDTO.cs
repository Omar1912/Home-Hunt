using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.DTOs
{
    public class UpdateProfileDTO
    {
        [MaxLength(20, ErrorMessage = "First Name cannot exceed 20 characters.")]
        [MinLength(3, ErrorMessage = "First Name must be at least 2 characters long.")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "Last Name cannot exceed 20 characters.")]
        [MinLength(3, ErrorMessage = "Last Name must be at least 2 characters long.")]
        public string LastName { get; set; } = string.Empty;


        [MaxLength(30, ErrorMessage = "Mobile number cannot exceed 30 characters.")]
        [RegularExpression(@"^\+?[\d- ]+$", ErrorMessage = "Mobile number must contain only digits, dashes, or a plus sign.")]
        public string? MobileNumber { get; set; }
    }
}
