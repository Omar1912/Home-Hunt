namespace HomeHunt.Models.DTOs
{
    public class FiltersDto
    {
        public string? City { get; set; } // Filter by city
        public string? village { get; set; } // Filter by city
        public float? MinPrice { get; set; } // Minimum price filter
        public float? MaxPrice { get; set; } // Maximum price filter
        public int? Bedrooms { get; set; } // Number of bedrooms
        public int? Bathrooms { get; set; } // Number of bathrooms
        public int? LivingRooms { get; set; } // Number of livingrooms
        public int? kitchens { get; set; } // Number of kitchen
        public string? Status { get; set; } // Filter by status ("For Rent", "For Sale")
        public string? HomeType { get; set; }//fitler by property type villa ,apartment...
        public string? RentDuration { get; set; } // Rent duration filter ("Monthly", "Annual")
        public int PageNumber { get; set; } = 1; // Default to 1 if not provided
        public int PageSize { get; set; } = 10;  // Default to 10 items per page
    }
}