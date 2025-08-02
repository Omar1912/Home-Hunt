using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.Entities
{
    public class ReportEntity
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ReporterId { get; set; }
        [ForeignKey("ReporterId")]
        public UserEntity Reporter { get; set; }
        [Required]
        public int PropertyId { get; set; }

        [ForeignKey("PropertyId")]
        public PropertyEntity Property { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}