namespace HomeHunt.Models.DTOs
{
    public class PropertyResponseDto
    {
        public int PropertyId { get; set; }
        public string Title { get; set; }
        public string City { get; set; }
        public string? Village { get; set; }
        public string Type { get; set; } 
        public double Price { get; set; }
        public string Status { get; set; }
        public string RentDuration { get; set; }
        public int Bedrooms { get; set; }
        public int Kitchens { get; set; }
        public int Bathrooms { get; set; }
        public int LivingRooms { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public string? ThemeImageUrl { get; set; }
        public int OwnerId { get; set; }
        public bool IsFavorite { get; set; }

    }

}
