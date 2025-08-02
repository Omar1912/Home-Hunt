using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace HomeHunt.Tests.Services
{
    public class PropertyServiceTests
    {
        private readonly HomeHuntDBContext _context;
        private readonly IPropertyService _propertyService;

        public PropertyServiceTests()
        {
            // Create InMemory database options
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: "HomeHuntTestDB_PropertyService")
                .Options;

            _context = new HomeHuntDBContext(options);

            // Make sure the DB is empty before every test
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _propertyService = new PropertyService(_context);
        }

        [Fact]
        public async Task PropertyExistsAsync_ReturnsTrue_WhenPropertyExistsAndIsNotDeleted()
        {
            // Arrange
            var property = new PropertyEntity { Id = 1, OwnerId = 10, IsDeleted = false };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _propertyService.PropertyExistsAsync(1);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task PropertyExistsAsync_ReturnsFalse_WhenPropertyIsDeleted()
        {
            // Arrange
            var property = new PropertyEntity { Id = 2, OwnerId = 20, IsDeleted = true };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _propertyService.PropertyExistsAsync(2);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task PropertyExistsAsync_ReturnsFalse_WhenPropertyDoesNotExist()
        {
            // Act
            var exists = await _propertyService.PropertyExistsAsync(999);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task GetOwnerIdByPropertyIdAsync_ReturnsOwnerId_WhenPropertyExists()
        {
            // Arrange
            var property = new PropertyEntity { Id = 3, OwnerId = 30, IsDeleted = false };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Act
            var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(3);

            // Assert
            Assert.Equal(30, ownerId);
        }

        [Fact]
        public async Task GetOwnerIdByPropertyIdAsync_ReturnsZero_WhenPropertyDoesNotExist()
        {
            // Act
            var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(999);

            // Assert
            Assert.Equal(0, ownerId); // Because FirstOrDefaultAsync returns default(int) => 0
        }
    }
}
