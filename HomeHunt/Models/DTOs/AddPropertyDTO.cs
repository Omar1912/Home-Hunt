 
using Microsoft.OpenApi.MicrosoftExtensions;
using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.DTOs
{
    public class AddPropertyDTO
    {
        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
        public double Price { get; set; }
        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = string.Empty;
        [MaxLength(1000, ErrorMessage = "Description can not exceed 1000 characters.")]
        public string? Description { get; set; }
        [MaxLength(50, ErrorMessage = "Village can not exceed 50 characters.")]
        public string? Village { get; set; }
        public float? Longitude { get; set; }
        public float? Latitude { get; set; }
        public string? Utilities { get; set; }
        public DateTime? AvailabilityDate { get; set; }
        public string? Policies { get; set; }
        [MaxLength(500, ErrorMessage = "Requirements can not exceed 500 characters.")]
        public string? Requirements { get; set; }
        public string? RentDuration { get; set; }
        [Range(0, 10, ErrorMessage = "Bedrooms must be between 0 and 10.")]
        public int Bedrooms { get; set; }
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(100, ErrorMessage = "Title can not exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one image is required.")]
        public List<IFormFile> ImageFiles { get; set; } = new();

        [MaxLength(100, ErrorMessage = "Street can not exceed 100 characters.")]
        public string Street { get; set; } = string.Empty;
        [Range(0, 5, ErrorMessage = "Kitchens must be between 0 and 5")]
        public int Kitchens { get; set; }
        [Range(0, 10, ErrorMessage = "Bathrooms must be between 0 and 10")]
        public int Bathrooms { get; set; }
        [Range(0, 5, ErrorMessage = "Living rooms must be between 0 and 5.")]
        public int LivingRooms { get; set; }
    }
}
