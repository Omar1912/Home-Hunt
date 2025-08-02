using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore.InMemory;

namespace HomeHuntTests_TourRequest
{
    public class TourRequestServiceUnitTests
    {
        private readonly HomeHuntDBContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IPropertyService> _propertyServiceMock;
        private readonly TourRequestService _service;

        public TourRequestServiceUnitTests()
        {
            var dbOptions = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new HomeHuntDBContext(dbOptions);
            _userServiceMock = new Mock<IUserService>();
            _propertyServiceMock = new Mock<IPropertyService>();

            _service = new TourRequestService(_context, _userServiceMock.Object, _propertyServiceMock.Object);
        }

        [Fact]
        public async Task CreateTourRequest_Succeeds_WithValidInput()
        {
            // Arrange
            int userId = 1;
            int propertyId = 10;
            int ownerId = 2;

            _context.Users.Add(new UserEntity { Id = userId, IsActive = true });
            _context.Users.Add(new UserEntity { Id = ownerId, IsActive = true });
            _context.Properties.Add(new PropertyEntity { Id = propertyId, OwnerId = ownerId });
            await _context.SaveChangesAsync();

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                            .ReturnsAsync(new UserEntity { Id = userId, IsActive = true });

            _userServiceMock.Setup(x => x.GetUserByIdAsync(ownerId))
                            .ReturnsAsync(new UserEntity { Id = ownerId, IsActive = true });

            _propertyServiceMock.Setup(x => x.PropertyExistsAsync(propertyId)).ReturnsAsync(true);
            _propertyServiceMock.Setup(x => x.GetOwnerIdByPropertyIdAsync(propertyId)).ReturnsAsync(ownerId);

            var dto = new TourRequestDTO
            {
                PropertyId = propertyId,
                PreferredDate1 = DateTime.UtcNow.AddDays(1).ToString(),
                PreferredDate2 = DateTime.UtcNow.AddDays(2).ToString(),
                PreferredDate3 = DateTime.UtcNow.AddDays(3).ToString(),
                Notes = "I'd love to visit."
            };

            // Act
            var result = await _service.CreateTourRequestAsync(userId, dto);

            // Assert
            Assert.Equal("You have successfully requested a tour.", result);
            Assert.Single(_context.TourRequests);

            var tour = await _context.TourRequests.FirstAsync();
            Assert.Equal(userId, tour.UserId);
            Assert.Equal(propertyId, tour.PropertyId);
            Assert.Equal("Pending", tour.Status);
        }

        [Fact]
        public async Task CreateTourRequest_Throws_WhenUserIsInactive()
        {
            // Arrange
            int userId = 1;

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new UserEntity { Id = userId, IsActive = false });

            var dto = new TourRequestDTO
            {
                PropertyId = 10,
                PreferredDate1 = DateTime.UtcNow.AddDays(1).ToString()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateTourRequestAsync(userId, dto));

            Assert.Contains("Your account is not active", ex.Message);
        }

        [Fact]
        public async Task CreateTourRequest_Throws_WhenPropertyDoesNotExist()
        {
            // Arrange
            int userId = 1;

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new UserEntity { Id = userId, IsActive = true });
            _propertyServiceMock.Setup(x => x.PropertyExistsAsync(999)).ReturnsAsync(false);

            var dto = new TourRequestDTO
            {
                PropertyId = 999,
                PreferredDate1 = DateTime.UtcNow.AddDays(1).ToString()
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateTourRequestAsync(userId, dto));

            Assert.Contains("Property not found", ex.Message);
        }

        [Fact]
        public async Task CreateTourRequest_Throws_WhenDatesAreInPast()
        {
            // Arrange
            int userId = 1;
            int propertyId = 10;
            int ownerId = 2;

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new UserEntity { Id = userId, IsActive = true });
            _userServiceMock.Setup(x => x.GetUserByIdAsync(ownerId)).ReturnsAsync(new UserEntity { Id = ownerId, IsActive = true });

            _propertyServiceMock.Setup(x => x.PropertyExistsAsync(propertyId)).ReturnsAsync(true);
            _propertyServiceMock.Setup(x => x.GetOwnerIdByPropertyIdAsync(propertyId)).ReturnsAsync(ownerId);

            var dto = new TourRequestDTO
            {
                PropertyId = propertyId,
                PreferredDate1 = DateTime.UtcNow.AddDays(-1).ToString(), // invalid date
                PreferredDate2 = null,
                PreferredDate3 = null
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateTourRequestAsync(userId, dto));

            Assert.Contains("Preferred dates must be in the future", ex.Message);
        }

        [Fact]
        public async Task CreateTourRequest_Throws_WhenDuplicateDatesProvided()
        {
            // Arrange
            int userId = 1;
            int propertyId = 10;
            int ownerId = 2;

            var date = DateTime.UtcNow.AddDays(1).ToString();

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(new UserEntity { Id = userId, IsActive = true });
            _userServiceMock.Setup(x => x.GetUserByIdAsync(ownerId)).ReturnsAsync(new UserEntity { Id = ownerId, IsActive = true });

            _propertyServiceMock.Setup(x => x.PropertyExistsAsync(propertyId)).ReturnsAsync(true);
            _propertyServiceMock.Setup(x => x.GetOwnerIdByPropertyIdAsync(propertyId)).ReturnsAsync(ownerId);

            var dto = new TourRequestDTO
            {
                PropertyId = propertyId,
                PreferredDate1 = date,
                PreferredDate2 = date,
                PreferredDate3 = null
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateTourRequestAsync(userId, dto));

            Assert.Contains("Duplicate dates are not allowed", ex.Message);
        }
    }
}


