namespace HomeHunt.Models.DTOs
{
    public class FiltersResponseDto<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<T> Properties { get; set; }
    }

}