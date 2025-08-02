using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHunt.Models.Entities
{
    public class PropertyImageEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [ForeignKey(nameof(PropertyId))]
        public PropertyEntity? Property { get; set; }

        [Required]
        [MaxLength(300)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsTheme { get; set; }
    }
}