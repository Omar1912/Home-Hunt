using System.ComponentModel.DataAnnotations;

public class TourRequestDTO
{
    [Required]
    public int PropertyId { get; set; }

    // Optional strings (null or whitespace allowed)
    public string? PreferredDate1 { get; set; }
    public string? PreferredDate2 { get; set; }
    public string? PreferredDate3 { get; set; }

    public string? Notes { get; set; }
}
