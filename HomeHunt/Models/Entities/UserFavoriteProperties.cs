namespace HomeHunt.Models.Entities
{
    public class UserFavoriteProperties
    {
        public int UserId { get; set; }
        public int PropertyId { get; set; }
        public virtual UserEntity User { get; set; } = null!;
        public virtual PropertyEntity Property { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    }
}
