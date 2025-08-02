using HomeHunt.Models.Entities;

public class TourEntity
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public int UserId { get; set; }

    public int OwnerId { get; set; }

    public DateTime? PreferredDate1 { get; set; }

    public DateTime? PreferredDate2 { get; set; }

    public DateTime? PreferredDate3 { get; set; }

    public string Notes { get; set; }

    public String Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; }
}
