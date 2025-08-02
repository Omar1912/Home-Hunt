using HomeHunt.Data;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace HomeHunt.Tests.Controllers
{
    // Define a record to match the anonymous type structure
    public record CityOrVillageResult(int id, string name);

    public class LocationControllerTests
    {
        private HomeHuntDBContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new HomeHuntDBContext(options);
        }

        private void SeedTestData(HomeHuntDBContext context)
        {
            var cities = new List<CityEntity>
            {
                new CityEntity { Id = 1, City = "Al-Quds", ImageUrl = "" },
                new CityEntity { Id = 2, City = "Al-Khalil", ImageUrl = "" },
                new CityEntity { Id = 3, City = "Yafa", ImageUrl = "" }
            };

            var villages = new List<VillageEntity>
            {
                new VillageEntity { Id = 1, Name = "Beit Hanina", CityId = 1 },
                new VillageEntity { Id = 2, Name = "Beit Surik", CityId = 1 },
                new VillageEntity { Id = 3, Name = "Deir Yassin", CityId = 2 }
            };

            context.Cities.AddRange(cities);
            context.Villages.AddRange(villages);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetCities_NoSearchTerm_ReturnsAllCities()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetCities(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var cities = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(cities);
            Assert.Equal(3, cities.Count());
        }

        [Fact]
        public async Task GetCities_WithSearchTerm_ReturnsFilteredCities()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetCities("Al");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var cities = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(cities);
            Assert.Equal(2, cities.Count());
            var cityNames = cities.Select(c => c.name).ToList();
            Assert.Contains("Al-Quds", cityNames);
            Assert.Contains("Al-Khalil", cityNames);
        }

        [Fact]
        public async Task GetCities_WithNonMatchingSearchTerm_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetCities("NonExistent");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var cities = okResult.Value as IEnumerable<dynamic>;
            Assert.NotNull(cities);
            Assert.Empty(cities);
        }

        [Fact]
        public async Task GetVillages_NoFilters_ReturnsAllVillages()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetVillages(null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var villages = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(villages);
            Assert.Equal(3, villages.Count());
        }

        [Fact]
        public async Task GetVillages_WithCityId_ReturnsFilteredVillages()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetVillages(1, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var villages = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(villages);
            Assert.Equal(2, villages.Count());
            var villageNames = villages.Select(v => v.name).ToList();
            Assert.Contains("Beit Hanina", villageNames);
            Assert.Contains("Beit Surik", villageNames);
        }

        [Fact]
        public async Task GetVillages_WithSearchTerm_ReturnsFilteredVillages()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetVillages(null, "Beit");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var villages = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(villages);
            Assert.Equal(2, villages.Count());
            var villageNames = villages.Select(v => v.name).ToList();
            Assert.Contains("Beit Hanina", villageNames);
            Assert.Contains("Beit Surik", villageNames);
        }

        [Fact]
        public async Task GetVillages_WithCityIdAndSearchTerm_ReturnsFilteredVillages()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetVillages(1, "Han");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonString = JsonSerializer.Serialize(okResult.Value);
            var villages = JsonSerializer.Deserialize<IEnumerable<CityOrVillageResult>>(jsonString);
            Assert.NotNull(villages);
            Assert.Single(villages);
            var village = villages.First();
            Assert.Equal("Beit Hanina", village.name);
        }

        [Fact]
        public async Task GetVillages_WithInvalidCityId_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            SeedTestData(context);
            var controller = new LocationController(context);

            // Act
            var result = controller.GetVillages(999, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var villages = okResult.Value as IEnumerable<dynamic>;
            Assert.NotNull(villages);
            Assert.Empty(villages);
        }
    }
}