using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore.Design;


namespace HomeHunt.Models.Entities
{
    public class PropertyEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public UserEntity? Owner { get; set; }
        [Required]
        public string City { get; set; } = string.Empty;
        [Required]
        public string Type { get; set; } = string.Empty; // (Apartment, House, Villa, ..)
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
        public double Price { get; set; }
        [Required]
        public string Status { get; set; } = string.Empty; // "For Rent", "For Sale"
        [MaxLength(1000)]
        public string? Description { get; set; }
        [Required]
        public DateTime CreatedAt { get; private set; } // Auto-set timestamp
        [MaxLength(50)]
        public string? Village { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Utilities { get; set; } //electricity, internet, water
        public DateTime? AvailabilityDate { get; set; }
        public string? Policies { get; set; } // Rental rules (no pets ..)
        [MaxLength(500)]
        public string? Requirements { get; set; }
        public string? RentDuration { get; set; } // "Monthly" or "Annual"
        [Range(0, 10)]
        public int Bedrooms { get; set; }
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Street { get; set; }
        [Range(0, 5)]
        public int Kitchens { get; set; }
        [Range(0, 10)]
        public int Bathrooms { get; set; }
        [Range(0, 5)]
        public int LivingRooms { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public ICollection<PropertyImageEntity> Images { get; set; } = new List<PropertyImageEntity>();
        public ICollection<UserFavoriteProperties> FavoritedBy { get; set; } = new List<UserFavoriteProperties>();
        public int ReportCount { get; set; } = 0;

        public PropertyEntity()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}

