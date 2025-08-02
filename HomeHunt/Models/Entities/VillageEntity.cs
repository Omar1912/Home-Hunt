using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHunt.Models.Entities
{
    public class VillageEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [ForeignKey("City")]
        public int CityId { get; set; }

        public CityEntity? City { get; set; }
    }
}
