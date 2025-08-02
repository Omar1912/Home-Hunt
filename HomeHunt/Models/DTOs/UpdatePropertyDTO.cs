using System.ComponentModel.DataAnnotations;

public class UpdatePropertyDTO
{
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string Type { get; set; } = string.Empty;
    [Range(0, double.MaxValue)]
    public double Price { get; set; }
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Village { get; set; }
    public float? Latitude { get; set; }
    public float? Longitude { get; set; }
    public string? Utilities { get; set; }
    public DateTime? AvailabilityDate { get; set; }
    public string? Policies { get; set; }
    public string? Requirements { get; set; }
    public string? RentDuration { get; set; }
    public int Bedrooms { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string? Street { get; set; }
    public int Kitchens { get; set; }
    public int Bathrooms { get; set; }
    public int LivingRooms { get; set; }
    public bool IsAvailable { get; set; }
    public List<IFormFile>? NewImageFiles { get; set; }
    public List<string>? ImageUrlsToKeep { get; set; }
}