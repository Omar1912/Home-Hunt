﻿using System.ComponentModel.DataAnnotations;
namespace HomeHunt.Models.Entities
{    public class CityEntity

    {

        [Key]

        public int Id { get; set; }

        [Required]

        public string City { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public ICollection<VillageEntity> Villages { get; set; } = new List<VillageEntity>();


    }

}

