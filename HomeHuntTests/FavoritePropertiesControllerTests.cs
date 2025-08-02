using HomeHunt.Controllers;
using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace HomeHuntTests
{
    public class FavoritePropertiesControllerTests : IDisposable
    {
        private readonly HomeHuntDBContext _context;
        private readonly FavoritePropertiesController _controller;
        private const int TestUserId = 1;
        private const int TestUserId2 = 2;
        private const int TestPropertyId = 1;
        private const int TestPropertyId2 = 2;
        private const int NonExistentPropertyId = 999;

        public FavoritePropertiesControllerTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HomeHuntDBContext(options);
            _controller = new FavoritePropertiesController(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test users
            var users = new List<UserEntity>
            {
                new UserEntity
                {
                    Id = TestUserId,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john@example.com",
                    UserName = "johndoe"
                },
                new UserEntity
                {
                    Id = TestUserId2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane@example.com",
                    UserName = "janesmith"
                }
            };

            // Add test properties
            var properties = new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    Id = TestPropertyId,
                    OwnerId = TestUserId2,
                    City = "New York",
                    Type = "Apartment",
                    Price = 2500,
                    Status = "For Rent",
                    Title = "Modern Apartment in NYC",
                    Description = "Beautiful apartment in downtown NYC",
                    RentDuration = "Monthly",
                    Bedrooms = 2,
                    IsAvailable = true
                },
                new PropertyEntity
                {
                    Id = TestPropertyId2,
                    OwnerId = TestUserId,
                    City = "Los Angeles",
                    Type = "House",
                    Price = 4000,
                    Status = "For Sale",
                    Title = "Family House in LA",
                    Description = "Spacious family house",
                    RentDuration = null,
                    Bedrooms = 3,
                    IsAvailable = true
                }
            };

            _context.Users.AddRange(users);
            _context.Properties.AddRange(properties);
            _context.SaveChanges();
        }

        private void SetupUserContext(int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region AddFavorite Tests

        [Fact]
        public async Task AddFavorite_WithValidData_ReturnsOkResult()
        {
            // Arrange
            SetupUserContext(TestUserId);
            var request = new FavoritePropertyDto { PropertyId = TestPropertyId };

            // Act
            var result = await _controller.AddFavorite(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Property added to favorites.", messageProperty.GetValue(response));

            // Verify the favorite was added to database
            var favorite = await _context.UserFavoriteProperties
                .FirstOrDefaultAsync(f => f.UserId == TestUserId && f.PropertyId == TestPropertyId);
            Assert.NotNull(favorite);
        }

        [Fact]
        public async Task AddFavorite_WithNonExistentProperty_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(TestUserId);
            var request = new FavoritePropertyDto { PropertyId = NonExistentPropertyId };

            // Act
            var result = await _controller.AddFavorite(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Property not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task AddFavorite_WithExistingFavorite_ReturnsBadRequest()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // First, add a favorite
            var existingFavorite = new UserFavoriteProperties
            {
                UserId = TestUserId,
                PropertyId = TestPropertyId
            };
            _context.UserFavoriteProperties.Add(existingFavorite);
            await _context.SaveChangesAsync();

            var request = new FavoritePropertyDto { PropertyId = TestPropertyId };

            // Act
            var result = await _controller.AddFavorite(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Property is already in your favorites.", badRequestResult.Value);
        }

        [Fact]
        public async Task AddFavorite_WithInvalidUserToken_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserContext(-1); // Invalid user ID that won't parse correctly
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "invalid") }));

            var request = new FavoritePropertyDto { PropertyId = TestPropertyId };

            // Act
            var result = await _controller.AddFavorite(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid user token.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task AddFavorite_WithMissingUserClaim_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // No claims
                }
            };

            var request = new FavoritePropertyDto { PropertyId = TestPropertyId };

            // Act
            var result = await _controller.AddFavorite(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid user token.", unauthorizedResult.Value);
        }

        #endregion

        #region DeleteFavorite Tests

        [Fact]
        public async Task DeleteFavorite_WithExistingFavorite_ReturnsOkResult()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // Add a favorite to delete
            var favorite = new UserFavoriteProperties
            {
                UserId = TestUserId,
                PropertyId = TestPropertyId
            };
            _context.UserFavoriteProperties.Add(favorite);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteFavorite(TestPropertyId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageProperty = response.GetType().GetProperty("Message");
            Assert.Equal("Property removed from favorites.", messageProperty.GetValue(response));

            // Verify the favorite was removed from database
            var deletedFavorite = await _context.UserFavoriteProperties
                .FirstOrDefaultAsync(f => f.UserId == TestUserId && f.PropertyId == TestPropertyId);
            Assert.Null(deletedFavorite);
        }

        [Fact]
        public async Task DeleteFavorite_WithNonExistentFavorite_ReturnsNotFound()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // Act
            var result = await _controller.DeleteFavorite(TestPropertyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Property is not in your favorites.", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteFavorite_WithInvalidUserToken_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "invalid") }))
                }
            };

            // Act
            var result = await _controller.DeleteFavorite(TestPropertyId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid user token.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task DeleteFavorite_UserCannotDeleteOtherUsersFavorites()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // Add a favorite for a different user
            var otherUserFavorite = new UserFavoriteProperties
            {
                UserId = TestUserId2,
                PropertyId = TestPropertyId
            };
            _context.UserFavoriteProperties.Add(otherUserFavorite);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteFavorite(TestPropertyId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Property is not in your favorites.", notFoundResult.Value);

            // Verify the other user's favorite is still there
            var stillExists = await _context.UserFavoriteProperties
                .AnyAsync(f => f.UserId == TestUserId2 && f.PropertyId == TestPropertyId);
            Assert.True(stillExists);
        }

        #endregion

        #region GetFavorites Tests
        [Fact]
        public async Task GetFavorites_WithNoFavorites_ReturnsEmptyList()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            var totalItemsProperty = response.GetType().GetProperty("totalItems");
            var propertiesProperty = response.GetType().GetProperty("properties");

            Assert.Equal(0, totalItemsProperty.GetValue(response));

            var properties = propertiesProperty.GetValue(response) as IEnumerable<object>;
            Assert.Empty(properties);
        }

        [Fact]
        public async Task GetFavorites_OnlyReturnsCurrentUserFavorites()
        {
            // Arrange
            SetupUserContext(TestUserId);

            // Add favorites for both users
            var favorites = new List<UserFavoriteProperties>
            {
                new UserFavoriteProperties { UserId = TestUserId, PropertyId = TestPropertyId },
                new UserFavoriteProperties { UserId = TestUserId2, PropertyId = TestPropertyId2 }
            };
            _context.UserFavoriteProperties.AddRange(favorites);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;

            var totalItemsProperty = response.GetType().GetProperty("totalItems");
            Assert.Equal(1, totalItemsProperty.GetValue(response)); // Only current user's favorite

            var propertiesProperty = response.GetType().GetProperty("properties");
            var properties = propertiesProperty.GetValue(response) as IEnumerable<object>;
            var propertyList = properties.ToList();

            // Should only return TestUserId's favorite (TestPropertyId)
            var returnedProperty = propertyList[0];
            var propertyId = returnedProperty.GetType().GetProperty("propertyId").GetValue(returnedProperty);
            Assert.Equal(TestPropertyId, propertyId);
        }

        [Fact]
        public async Task GetFavorites_WithInvalidUserToken_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "invalid") }))
                }
            };

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid user token.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task GetFavorites_ReturnsCorrectPropertyFields()
        {
            // Arrange
            SetupUserContext(TestUserId);

            var favorite = new UserFavoriteProperties { UserId = TestUserId, PropertyId = TestPropertyId };
            _context.UserFavoriteProperties.Add(favorite);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var propertiesProperty = response.GetType().GetProperty("properties");
            var properties = propertiesProperty.GetValue(response) as IEnumerable<object>;
            var property = properties.First();

            // Verify all expected fields are present
            var propertyType = property.GetType();
            Assert.NotNull(propertyType.GetProperty("propertyId"));
            Assert.NotNull(propertyType.GetProperty("city"));
            Assert.NotNull(propertyType.GetProperty("price"));
            Assert.NotNull(propertyType.GetProperty("status"));
            Assert.NotNull(propertyType.GetProperty("rentDuration"));
            Assert.NotNull(propertyType.GetProperty("bedrooms"));
            Assert.NotNull(propertyType.GetProperty("description"));
            Assert.NotNull(propertyType.GetProperty("isAvailable"));

            // Verify field values
            Assert.Equal("New York", propertyType.GetProperty("city").GetValue(property));
            Assert.Equal(2500.0, propertyType.GetProperty("price").GetValue(property));
            Assert.Equal("For Rent", propertyType.GetProperty("status").GetValue(property));
            Assert.Equal("Monthly", propertyType.GetProperty("rentDuration").GetValue(property));
            Assert.Equal(2, propertyType.GetProperty("bedrooms").GetValue(property));
            Assert.Equal("Beautiful apartment in downtown NYC", propertyType.GetProperty("description").GetValue(property));
            Assert.Equal(true, propertyType.GetProperty("isAvailable").GetValue(property));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task FullWorkflow_AddAndDeleteFavorite_WorksCorrectly()
        {
            // Arrange
            SetupUserContext(TestUserId);
            var addRequest = new FavoritePropertyDto { PropertyId = TestPropertyId };

            // Act & Assert - Add favorite
            var addResult = await _controller.AddFavorite(addRequest);
            Assert.IsType<OkObjectResult>(addResult);

            // Verify it's in favorites list
            var getFavoritesResult = await _controller.GetFavorites();
            var okResult = Assert.IsType<OkObjectResult>(getFavoritesResult);
            var response = okResult.Value;
            var totalItems = response.GetType().GetProperty("totalItems").GetValue(response);
            Assert.Equal(1, totalItems);

            // Act & Assert - Delete favorite
            var deleteResult = await _controller.DeleteFavorite(TestPropertyId);
            Assert.IsType<OkObjectResult>(deleteResult);

            // Verify it's no longer in favorites list
            var getFavoritesAfterDelete = await _controller.GetFavorites();
            var okResultAfterDelete = Assert.IsType<OkObjectResult>(getFavoritesAfterDelete);
            var responseAfterDelete = okResultAfterDelete.Value;
            var totalItemsAfterDelete = responseAfterDelete.GetType().GetProperty("totalItems").GetValue(responseAfterDelete);
            Assert.Equal(0, totalItemsAfterDelete);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

// Remove this DTO definition since it already exists in the main project