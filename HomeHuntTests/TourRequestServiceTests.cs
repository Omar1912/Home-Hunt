using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace HomeHunt.Tests.Services
{
    public class TourRequestServiceTests
    {
        private readonly HomeHuntDBContext _context;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IPropertyService> _mockPropertyService;
        private readonly TourRequestService _tourRequestService;

        public TourRequestServiceTests()
        {
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique db for each test
                .Options;

            _context = new HomeHuntDBContext(options);

            _mockUserService = new Mock<IUserService>();
            _mockPropertyService = new Mock<IPropertyService>();

            _tourRequestService = new TourRequestService(_context, _mockUserService.Object, _mockPropertyService.Object);
        }

        [Fact]
        public async Task CreateTourRequestAsync_ShouldCreateTour_WhenDataIsValid()
        {
            // Arrange
            var userId = 1;
            var propertyId = 10;
            var ownerId = 2;

            var tourRequestDto = new TourRequestDTO
            {
                PropertyId = propertyId,
                PreferredDate1 = DateTime.UtcNow.AddDays(1).ToString(),
                PreferredDate2 = DateTime.UtcNow.AddDays(2).ToString(),
                PreferredDate3 = DateTime.UtcNow.AddDays(3).ToString(),
                Notes = "Test notes"
            };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _mockPropertyService.Setup(x => x.PropertyExistsAsync(propertyId))
                                 .ReturnsAsync(true);

            _mockPropertyService.Setup(x => x.GetOwnerIdByPropertyIdAsync(propertyId))
                                 .ReturnsAsync(ownerId);

            _mockUserService.Setup(x => x.GetUserByIdAsync(ownerId))
                            .ReturnsAsync(new UserEntity { Id = ownerId, IsActive = true });

            // Act
            var result = await _tourRequestService.CreateTourRequestAsync(userId, tourRequestDto);

            // Assert
            Assert.Equal("You have successfully requested a tour.", result);

            var createdTour = await _context.TourRequests.FirstOrDefaultAsync();
            Assert.NotNull(createdTour);
            Assert.Equal(propertyId, createdTour.PropertyId);
            Assert.Equal(userId, createdTour.UserId);
            Assert.Equal(ownerId, createdTour.OwnerId);
            Assert.Equal("Pending", createdTour.Status);
        }

        [Fact]
        public async Task ValidateTourRequestAsync_ShouldThrow_WhenUserIsInactive()
        {
            // Arrange
            var userId = 1;
            var dto = new TourRequestDTO { PropertyId = 10 };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = false });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tourRequestService.ValidateTourRequestAsync(userId, dto));
            Assert.Equal("Your account is not active or does not exist.", ex.Message);
        }

        [Fact]
        public async Task ValidateTourRequestAsync_ShouldThrow_WhenPropertyNotFound()
        {
            // Arrange
            var userId = 1;
            var dto = new TourRequestDTO { PropertyId = 99 };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _mockPropertyService.Setup(x => x.PropertyExistsAsync(dto.PropertyId))
                                 .ReturnsAsync(false);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tourRequestService.ValidateTourRequestAsync(userId, dto));
            Assert.Equal("Property not found.", ex.Message);
        }

        [Fact]
        public async Task ValidateTourRequestAsync_ShouldThrow_WhenOwnerNotFound()
        {
            // Arrange
            var userId = 1;
            var dto = new TourRequestDTO { PropertyId = 10 };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _mockPropertyService.Setup(x => x.PropertyExistsAsync(dto.PropertyId))
                                 .ReturnsAsync(true);

            _mockPropertyService.Setup(x => x.GetOwnerIdByPropertyIdAsync(dto.PropertyId))
                                 .ReturnsAsync(0);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tourRequestService.ValidateTourRequestAsync(userId, dto));
            Assert.Equal("Owner not found for the specified property.", ex.Message);
        }

        [Fact]
        public async Task ValidateTourRequestAsync_ShouldThrow_WhenOwnerIsInactive()
        {
            // Arrange
            var userId = 1;
            var dto = new TourRequestDTO { PropertyId = 10 };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _mockPropertyService.Setup(x => x.PropertyExistsAsync(dto.PropertyId))
                                 .ReturnsAsync(true);

            _mockPropertyService.Setup(x => x.GetOwnerIdByPropertyIdAsync(dto.PropertyId))
                                 .ReturnsAsync(2);

            _mockUserService.Setup(x => x.GetUserByIdAsync(2))
                            .ReturnsAsync(new UserEntity { Id = 2, IsActive = false });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tourRequestService.ValidateTourRequestAsync(userId, dto));
            Assert.Equal("Property owner is not active.", ex.Message);
        }

        [Fact]
        public async Task ValidateTourRequestAsync_ShouldThrow_WhenDatesAreInvalid()
        {
            // Arrange
            var userId = 1;
            var propertyId = 10;

            var dto = new TourRequestDTO
            {
                PropertyId = propertyId,
                PreferredDate1 = DateTime.UtcNow.AddDays(-1).ToString(), // Invalid (past date)
                PreferredDate2 = DateTime.UtcNow.AddDays(2).ToString(),
                PreferredDate3 = DateTime.UtcNow.AddDays(3).ToString()
            };

            _mockUserService.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _mockPropertyService.Setup(x => x.PropertyExistsAsync(dto.PropertyId))
                                 .ReturnsAsync(true);

            _mockPropertyService.Setup(x => x.GetOwnerIdByPropertyIdAsync(dto.PropertyId))
                                 .ReturnsAsync(2);

            _mockUserService.Setup(x => x.GetUserByIdAsync(2))
                            .ReturnsAsync(new UserEntity { Id = 2, IsActive = true });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _tourRequestService.ValidateTourRequestAsync(userId, dto));
            Assert.Equal("Preferred dates must be in the future.", ex.Message);
        }
    }
}
