using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.Entities
{
    public class UserEntity : IdentityUser<int>
    {
        [Required]
        [MaxLength(20)]
        [MinLength(3)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [MinLength(3)]

        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        [RegularExpression(@"^\+?[\d- ]+$", ErrorMessage = "Mobile number must contain only digits, dashes, or a plus sign.")]
        public string? MobileNumber { get; set; }

        [Required]
        public DateTime CreatedAt { get; private set; }
 
        public bool IsActive { get; set; } = true;
        public ICollection<UserFavoriteProperties> FavoriteProperties { get; set; } = new List<UserFavoriteProperties>();
        public int StrikeCount { get; set; } = 0;
        public UserEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}