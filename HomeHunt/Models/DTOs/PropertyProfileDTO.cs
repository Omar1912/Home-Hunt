namespace HomeHunt.Models.DTOs
{
    public class PropertyProfileDto
    {
        public int Id { get; set; }
        public int ownerId { get; set; }
        public OwnerDto? Owner { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Village { get; set; }
        public string? Street { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Utilities { get; set; }
        public DateTime? AvailabilityDate { get; set; }
        public string? Policies { get; set; }
        public string? Requirements { get; set; }
        public string? RentDuration { get; set; }
        public int Bedrooms { get; set; }
        public int Kitchens { get; set; }
        public int Bathrooms { get; set; }
        public int LivingRooms { get; set; }
        public bool IsAvailable { get; set; }
        public List<ImageDto> Images { get; set; } = new();
        public bool IsFavoritedByCurrentUser { get; set; }
    }

}
