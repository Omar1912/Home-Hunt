using HomeHunt.Controllers;
using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace HomeHuntTests
{
    public class PropertyControllerTests
    {
        private readonly HomeHuntDBContext _context;
        private readonly PropertyController _controller;
        private object _filterServiceMock;

        public PropertyControllerTests()
        {
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HomeHuntDBContext(options);

            var filterServiceMock = new Mock<IPropertyFilterService>();

            _controller = new PropertyController(_context, filterServiceMock.Object);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task AddProperty_ValidInput_ReturnsOkAndSavesProperty()
        {
            var content = "FakeImageContent";
            var fileName = "test.jpg";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(ms, 0, ms.Length, "ImageFiles", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var dto = new AddPropertyDTO
            {
                City = "Ramallah",
                Type = "Apartment",
                Price = 500,
                Status = "For Rent",
                Title = "Nice Apartment",
                Street = "Main St",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 1,
                Bedrooms = 2,
                ImageFiles = new List<IFormFile> { formFile }
            };

            var result = await _controller.AddProperty(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var savedProperty = _context.Properties.FirstOrDefault();
            Assert.NotNull(savedProperty);
            Assert.Equal("Ramallah", savedProperty.City);
        }

        [Fact]
        public async Task AddProperty_MissingRequiredFields_ReturnsBadRequest()
        {
            var dto = new AddPropertyDTO();

            _controller.ModelState.AddModelError("City", "Required");

            var result = await _controller.AddProperty(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task AddProperty_NoImagesProvided_ReturnsBadRequest()
        {
            var dto = new AddPropertyDTO
            {
                City = "Ramallah",
                Type = "House",
                Price = 700,
                Status = "For Rent",
                Title = "Modern House",
                Street = "Garden St",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 2,
                Bedrooms = 3,
                ImageFiles = new List<IFormFile>()
            };

            var result = await _controller.AddProperty(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value, null)?.ToString();
            Assert.Equal("At least one image is required.", error);
        }

        [Fact]
        public async Task AddProperty_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var controller = new PropertyController(_context, new Mock<IPropertyFilterService>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var content = "FakeImageContent";
            var fileName = "test.jpg";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var formFile = new FormFile(ms, 0, ms.Length, "ImageFiles", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var dto = new AddPropertyDTO
            {
                City = "Jericho",
                Type = "Villa",
                Price = 1000,
                Status = "For Sale",
                Title = "Luxury Villa",
                Street = "Palm Road",
                Kitchens = 2,
                Bathrooms = 3,
                LivingRooms = 2,
                Bedrooms = 4,
                ImageFiles = new List<IFormFile> { formFile }
            };

            var result = await controller.AddProperty(dto);

            var objectResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
            var error = objectResult.Value?.GetType().GetProperty("error")?.GetValue(objectResult.Value, null)?.ToString();
            Assert.Equal("User ID claim not found in token.", error);
        }

        [Fact]
        public async Task UpdateProperty_ValidInput_UpdatesAndReturnsOk()
        {
            var property = new PropertyEntity
            {
                Id = 100,
                OwnerId = 1,
                City = "OldCity",
                Type = "House",
                Price = 200,
                Status = "For Sale",
                Title = "Old Title",
                Street = "Old Street"
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new UpdatePropertyDTO
            {
                City = "NewCity",
                Type = "Villa",
                Price = 300,
                Status = "For Rent",
                Title = "New Title",
                Street = "New Street",
                Kitchens = 1,
                Bathrooms = 2,
                LivingRooms = 1,
                Bedrooms = 3,
                IsAvailable = true,
                ImageUrlsToKeep = new List<string>(),
                NewImageFiles = new List<IFormFile>()
            };

            var result = await _controller.UpdateProperty(100, dto);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("NewCity", _context.Properties.Find(100)?.City);
        }

        [Fact]
        public async Task UpdateProperty_NotOwner_ReturnsForbidden()
        {
            var property = new PropertyEntity
            {
                Id = 200,
                OwnerId = 99,
                City = "City",
                Type = "House",
                Price = 100,
                Status = "For Sale",
                Title = "Title",
                Street = "Street"
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new UpdatePropertyDTO
            {
                City = "City",
                Type = "House",
                Price = 100,
                Status = "For Sale",
                Title = "Title",
                Street = "Street",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 1,
                Bedrooms = 2,
                IsAvailable = true
            };

            var result = await _controller.UpdateProperty(200, dto);
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
        }

        [Fact]
        public async Task UpdateProperty_NotFound_ReturnsNotFound()
        {
            var dto = new UpdatePropertyDTO
            {
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "Title",
                Street = "Street",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 1,
                Bedrooms = 1,
                IsAvailable = true
            };

            var result = await _controller.UpdateProperty(9999, dto);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var message = notFound.Value?.GetType().GetProperty("message")?.GetValue(notFound.Value, null)?.ToString();
            Assert.Equal("Property with ID 9999 not found or has been deleted.", message);
        }


        [Fact]
        public async Task UpdateProperty_InvalidModelState_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("City", "Required");

            var dto = new UpdatePropertyDTO
            {
                City = "",
                Type = "Villa",
                Price = 500,
                Status = "For Rent",
                Title = "Villa",
                Street = "Palm St",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 1,
                Bedrooms = 2,
                IsAvailable = true
            };

            var result = await _controller.UpdateProperty(1, dto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task UpdateProperty_EnsuresCoverImage_WhenNoneExists()
        {
            var property = new PropertyEntity
            {
                Id = 300,
                OwnerId = 1,
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "Title",
                Street = "Street",
                Images = new List<PropertyImageEntity>
        {
            new PropertyImageEntity
            {
                ImageUrl = "/images/1.jpg",
                IsTheme = false,
                PropertyId = 300
            }
        }
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new UpdatePropertyDTO
            {
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "Title",
                Street = "Street",
                Kitchens = 1,
                Bathrooms = 1,
                LivingRooms = 1,
                Bedrooms = 2,
                IsAvailable = true,
                ImageUrlsToKeep = new List<string> { "/images/1.jpg" }
            };

            var result = await _controller.UpdateProperty(300, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var updatedImage = _context.PropertyImages.FirstOrDefault(img => img.PropertyId == 300);
            Assert.NotNull(updatedImage);
            Assert.True(updatedImage.IsTheme);
        }

        [Fact]
        public async Task DeleteProperty_ValidOwner_MarksAsDeleted()
        {
            var property = new PropertyEntity
            {
                Id = 400,
                OwnerId = 1,
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "My Property",
                Street = "Street",
                IsDeleted = false
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteProperty(400);

            var ok = Assert.IsType<OkObjectResult>(result);
            var updated = await _context.Properties.FindAsync(400);
            Assert.True(updated.IsDeleted);
        }

        [Fact]
        public async Task DeleteProperty_NotOwner_ReturnsForbidden()
        {
            var property = new PropertyEntity
            {
                Id = 401,
                OwnerId = 999,
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "Not Mine",
                Street = "Street",
                IsDeleted = false
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteProperty(401);

            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, forbidden.StatusCode);
            var error = forbidden.Value?.GetType().GetProperty("error")?.GetValue(forbidden.Value, null)?.ToString();
            Assert.Equal("You are not authorized to delete this property.", error);
        }

        [Fact]
        public async Task DeleteProperty_NotFound_ReturnsNotFound()
        {
            var result = await _controller.DeleteProperty(9999);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var msg = notFound.Value?.GetType().GetProperty("message")?.GetValue(notFound.Value, null)?.ToString();
            Assert.Equal("Property with ID 9999 not found.", msg);
        }

        [Fact]
        public async Task DeleteProperty_UnauthenticatedUser_ReturnsUnauthorized()
        {
            var property = new PropertyEntity
            {
                Id = 999,
                OwnerId = 1,
                City = "City",
                Type = "Type",
                Price = 100,
                Status = "For Sale",
                Title = "Unauth Property",
                Street = "Street",
                IsDeleted = false
            };
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var controller = new PropertyController(_context, new Mock<IPropertyFilterService>().Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await controller.DeleteProperty(999);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            var error = unauthorized.Value?.GetType().GetProperty("error")?.GetValue(unauthorized.Value, null)?.ToString();
            Assert.Equal("Invalid or missing user claim.", error);
        }

        [Fact]
        public async Task GetProperty_ValidId_ReturnsProperty()
        {
            var property = new PropertyEntity
            {
                Id = 500,
                OwnerId = 1,
                City = "Jerusalem",
                Type = "Apartment",
                Price = 900,
                Status = "For Sale",
                Title = "Valid Property",
                Street = "Main Street",
                IsDeleted = false,
                Images = new List<PropertyImageEntity>
        {
            new PropertyImageEntity { ImageUrl = "/images/pic1.jpg", IsTheme = true }
        }
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var result = await _controller.GetProperty(500);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<PropertyEntity>(ok.Value);
            Assert.Equal("Jerusalem", returned.City);
        }

        [Fact]
        public async Task GetProperty_PropertyDeleted_ReturnsBadRequest()
        {
            var property = new PropertyEntity
            {
                Id = 501,
                OwnerId = 1,
                City = "Hebron",
                Type = "House",
                Price = 700,
                Status = "For Rent",
                Title = "Deleted Property",
                Street = "Old Street",
                IsDeleted = true
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var result = await _controller.GetProperty(501);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value, null)?.ToString();
            Assert.Equal("Property not found", error);
        }

        [Fact]
        public async Task GetProperty_NotFound_ReturnsBadRequest()
        {
            var result = await _controller.GetProperty(9999);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value, null)?.ToString();
            Assert.Equal("Property not found", error);
        }

        // ----------- PrivatePropertiesSearch tests -----------

        [Fact]
        public void PrivatePropertiesSearch_MissingUserId_ReturnsUnauthorized()
        {
            // Arrange
            var filters = new PrivateFiltersDTO { PageNumber = 1, PageSize = 10 };

            // Remove user claims to simulate missing userId
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = _controller.PrivatePropertiesSearch(filters);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);

            var responseDict = unauthorizedResult.Value?.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(unauthorizedResult.Value));

            Assert.NotNull(responseDict);
            Assert.False((bool)responseDict["success"]);
            Assert.Equal("User ID not found in token.", responseDict["message"]);
        }

        [Fact]
        public void PrivatePropertiesSearch_WithNullFilters_ReturnsBadRequest()
        {
            // Act
            var result = _controller.PrivatePropertiesSearch(null);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Filter parameters are required.", badRequest.Value);
        }
        [Fact]
        public void PrivatePropertiesSearch_WithoutFilters_ReturnsAllUserProperties()
        {
            // Arrange
            var userId = 2;

            // Add user to context
            _context.Users.Add(new UserEntity
            {
                Id = userId,
                IsActive = true,
                PhoneNumber = "123456789"
            });

            // Add properties for the user
            _context.Properties.AddRange(
                new PropertyEntity
                {
                    Id = 3,
                    Title = "Property One",
                    OwnerId = userId,
                    IsDeleted = false,
                    IsAvailable = true,
                    Status = "For Sale",
                    Bedrooms = 2,
                    Bathrooms = 1,
                    Kitchens = 1,
                    LivingRooms = 1,
                    City = "Ramallah",
                    Description = "Nice home",
                    Images = new List<PropertyImageEntity>
                    {
                new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/img1.jpg" }
                    }
                },
                new PropertyEntity
                {
                    Id = 4,
                    Title = "Property Two",
                    OwnerId = userId,
                    IsDeleted = false,
                    IsAvailable = true,
                    Status = "For Rent",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    Kitchens = 1,
                    LivingRooms = 1,
                    City = "Hebron",
                    Description = "Great view",
                    Images = new List<PropertyImageEntity>
                    {
                new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/img2.jpg" }
                    }
                }
            );

            _context.SaveChanges();

            // Simulate authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock filter service to return all matching properties
            var filterServiceMock = new Mock<IPropertyFilterService>();
            filterServiceMock
                .Setup(s => s.ApplyFilters(It.IsAny<IQueryable<PropertyEntity>>(), It.IsAny<PrivateFiltersDTO>()))
                .Returns((IQueryable<PropertyEntity> source, PrivateFiltersDTO _) => source);

            var controller = new PropertyController(_context, filterServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };

            var filters = new PrivateFiltersDTO(); // No filters set

            // Act
            var result = controller.PrivatePropertiesSearch(filters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<FiltersResponseDto>(okResult.Value);
            Assert.Equal(1, response.PageNumber); // Default value if not set
            Assert.Equal(10, response.PageSize);  // Default value if not set
            Assert.Equal(1, response.TotalPages); // 2 items / 10 per page
            Assert.Equal(2, response.TotalItems);
            Assert.Equal(2, response.Properties.Count);

            Assert.Contains(response.Properties, p => p.PropertyId == 3);
            Assert.Contains(response.Properties, p => p.PropertyId == 4);
        }
        [Fact]
        public void PrivatePropertiesSearch_WithCustomFilters_ReturnsFilteredUserProperties()
        {
            // Arrange
            var userId = 2;

            // Add user to context
            _context.Users.Add(new UserEntity
            {
                Id = userId,
                IsActive = true,
                PhoneNumber = "123456789"
            });

            // Add multiple properties
            var property1 = new PropertyEntity
            {
                Id = 10,
                Title = "Luxury Villa in Ramallah",
                OwnerId = userId,
                IsDeleted = false,
                IsAvailable = true,
                Status = "For Sale",
                Bedrooms = 4,
                Bathrooms = 3,
                Kitchens = 1,
                LivingRooms = 2,
                City = "Ramallah",
                Price = 500000,
                Description = "Luxury villa with a pool",
                Images = new List<PropertyImageEntity>
        {
            new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/villa.jpg" }
        }
            };

            var property2 = new PropertyEntity
            {
                Id = 11,
                Title = "Studio in Hebron",
                OwnerId = userId,
                IsDeleted = false,
                IsAvailable = true,
                Status = "For Rent",
                Bedrooms = 1,
                Bathrooms = 1,
                Kitchens = 1,
                LivingRooms = 0,
                City = "Hebron",
                Price = 300,
                RentDuration = "Monthly",
                Description = "Small studio apartment",
                Images = new List<PropertyImageEntity>
        {
            new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/studio.jpg" }
        }
            };

            _context.Properties.AddRange(property1, property2);
            _context.SaveChanges();

            // Simulate authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            // Mock filter service to apply custom filters (e.g., only "For Sale" properties in Ramallah)
            var filterServiceMock = new Mock<IPropertyFilterService>();
            filterServiceMock
                .Setup(s => s.ApplyFilters(It.IsAny<IQueryable<PropertyEntity>>(), It.IsAny<PrivateFiltersDTO>()))
                .Returns((IQueryable<PropertyEntity> source, PrivateFiltersDTO filters) =>
                    source.Where(p => p.Status.ToLower().Contains(filters.Status.ToLower()) &&
                                      p.City.ToLower().Contains(filters.City.ToLower()))
                );

            var controller = new PropertyController(_context, filterServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };

            var filters = new PrivateFiltersDTO
            {
                PageNumber = 1,
                PageSize = 10,
                Status = "for sale",
                City = "ramallah"
            };

            // Act
            var result = controller.PrivatePropertiesSearch(filters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<FiltersResponseDto>(okResult.Value);
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(10, response.PageSize);
            Assert.Equal(1, response.TotalPages);
            Assert.Equal(1, response.TotalItems);
            Assert.Single(response.Properties);

            var returnedProperty = response.Properties.First();
            Assert.Equal(10, returnedProperty.PropertyId);
            Assert.Equal("Luxury Villa in Ramallah", returnedProperty.Title);
            Assert.Equal("Ramallah", returnedProperty.City);
            Assert.Equal("123456789", returnedProperty.PhoneNumber); // Private search
        }


        //public search unit tests

        [Fact]
        public void PublicSearch_NullFilters_ReturnsBadRequest()
        {
            var result = _controller.PublicSearch(null);

            Assert.IsType<BadRequestObjectResult>(result);
            var badRequest = result as BadRequestObjectResult;
            Assert.Equal("Filter parameters are required.", badRequest.Value);
        }

        [Fact]
        public void PublicSearch_NegativeMinPrice_ReturnsBadRequest()
        {
            var filters = new FiltersDto { MinPrice = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Price cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_NegativeMaxPrice_ReturnsBadRequest()
        {
            var filters = new FiltersDto { MaxPrice = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Price cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_NegativeBedrooms_ReturnsBadRequest()
        {
            var filters = new FiltersDto { Bedrooms = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Bedrooms cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_NegativeBathrooms_ReturnsBadRequest()
        {
            var filters = new FiltersDto { Bathrooms = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Bathrooms cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_NegativeKitchens_ReturnsBadRequest()
        {
            var filters = new FiltersDto { kitchens = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("kitchens cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_NegativeLivingRooms_ReturnsBadRequest()
        {
            var filters = new FiltersDto { LivingRooms = -1 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("living rooms  cannot be negative.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_MinPriceGreaterThanMaxPrice_ReturnsBadRequest()
        {
            var filters = new FiltersDto { MinPrice = 1000, MaxPrice = 500 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("MinPrice cannot be greater than MaxPrice.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_InvalidStatus_ReturnsBadRequest()
        {
            var filters = new FiltersDto { Status = "invalidstatus" };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid Status. It must be either 'for rent' or 'for sale'.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_InvalidRentDurationForRentStatus_ReturnsBadRequest()
        {
            var filters = new FiltersDto { Status = "for rent", RentDuration = "yearly" };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid Rent Duration. It must be either 'Monthly' or 'Annual' or Weekly.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_RentDurationProvidedForNonRentStatus_ReturnsBadRequest()
        {
            var filters = new FiltersDto { Status = "for sale", RentDuration = "monthly" };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Rent Duration should only be provided for properties that are for rent.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_PageNumberLessThanOrEqualZero_ReturnsBadRequest()
        {
            var filters = new FiltersDto { PageNumber = 0 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("PageNumber must be greater than 0.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_PageSizeLessThanOrEqualZero_ReturnsBadRequest()
        {
            var filters = new FiltersDto { PageSize = 0 };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("PageSize must be greater than 0.", (result as BadRequestObjectResult).Value);
        }

        [Fact]
        public void PublicSearch_InvalidHomeType_ReturnsBadRequest()
        {
            var filters = new FiltersDto { HomeType = "castle" };
            var result = _controller.PublicSearch(filters);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Sorry, we don't have any properties in the type u just searched", (result as BadRequestObjectResult).Value);
        }
        [Fact]
        public void PublicSearch_NoFiltersApplied_ReturnsAllAvailableProperties()
        {
            // Arrange
            // Add some active users
            _context.Users.AddRange(
                new UserEntity { Id = 1, IsActive = true, PhoneNumber = "111111111" },
                new UserEntity { Id = 2, IsActive = true, PhoneNumber = "222222222" },
                new UserEntity { Id = 3, IsActive = false, PhoneNumber = "333333333" } // Inactive user
            );

            // Add properties linked to active and inactive users
            _context.Properties.AddRange(
                new PropertyEntity
                {
                    Id = 10,
                    Title = "Available Property 1",
                    OwnerId = 1,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Sale",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    City = "Ramallah",
                    Images = new List<PropertyImageEntity> { new PropertyImageEntity { IsTheme = true, ImageUrl = "/img1.jpg" } }
                },
                new PropertyEntity
                {
                    Id = 11,
                    Title = "Available Property 2",
                    OwnerId = 2,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Rent",
                    Bedrooms = 2,
                    Bathrooms = 1,
                    City = "Hebron",
                    Images = new List<PropertyImageEntity> { new PropertyImageEntity { IsTheme = true, ImageUrl = "/img2.jpg" } }
                },
                new PropertyEntity
                {
                    Id = 12,
                    Title = "Unavailable Property",
                    OwnerId = 1,
                    IsAvailable = false,
                    IsDeleted = false,
                    Status = "For Sale",
                    Bedrooms = 1,
                    Bathrooms = 1,
                    City = "Nablus",
                    Images = new List<PropertyImageEntity> { new PropertyImageEntity { IsTheme = true, ImageUrl = "/img3.jpg" } }
                },
                new PropertyEntity
                {
                    Id = 13,
                    Title = "Deleted Property",
                    OwnerId = 2,
                    IsAvailable = true,
                    IsDeleted = true,
                    Status = "For Sale",
                    Bedrooms = 4,
                    Bathrooms = 3,
                    City = "Jerusalem",
                    Images = new List<PropertyImageEntity> { new PropertyImageEntity { IsTheme = true, ImageUrl = "/img4.jpg" } }
                },
                new PropertyEntity
                {
                    Id = 14,
                    Title = "Property with Inactive Owner",
                    OwnerId = 3,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Sale",
                    Bedrooms = 5,
                    Bathrooms = 4,
                    City = "Bethlehem",
                    Images = new List<PropertyImageEntity> { new PropertyImageEntity { IsTheme = true, ImageUrl = "/img5.jpg" } }
                }
            );

            _context.SaveChanges();

            // Setup filters with default values (page 1, page size 10)
            var filters = new FiltersDto();

            // Mock the filter service to just return the query without filtering
            var filterServiceMock = new Mock<IPropertyFilterService>();
            filterServiceMock
                .Setup(s => s.ApplyFilters(It.IsAny<IQueryable<PropertyEntity>>(), It.IsAny<FiltersDto>()))
                .Returns((IQueryable<PropertyEntity> source, FiltersDto _) => source);

            var controller = new PropertyController(_context, filterServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = controller.PublicSearch(filters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<FiltersResponseDto>(okResult.Value);

            // Only properties that are available, not deleted, and whose owner is active should be returned
            // So Property IDs 10 and 11 only
            Assert.Equal(2, response.TotalItems);
            Assert.Contains(response.Properties, p => p.PropertyId == 10);
            Assert.Contains(response.Properties, p => p.PropertyId == 11);

            // Default pagination values
            Assert.Equal(1, response.PageNumber);
            Assert.Equal(10, response.PageSize);
        }
        [Fact]
        public void PublicSearch_WithCustomFilters_ReturnsFilteredProperties()
        {
            // Arrange
            // Add active user
            _context.Users.Add(new UserEntity
            {
                Id = 1,
                IsActive = true,
                PhoneNumber = "555-1234"
            });

            // Add properties with different attributes
            _context.Properties.AddRange(
                new PropertyEntity
                {
                    Id = 20,
                    Title = "Family Home",
                    OwnerId = 1,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Sale",
                    Bedrooms = 3,
                    Bathrooms = 2,
                    City = "Ramallah",
                    Price = 250000,
                    Type = "House",
                    Images = new List<PropertyImageEntity>
                    {
                new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/familyhome.jpg" }
                    }
                },
                new PropertyEntity
                {
                    Id = 21,
                    Title = "Modern Apartment",
                    OwnerId = 1,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Rent",
                    Bedrooms = 2,
                    Bathrooms = 1,
                    City = "Ramallah",
                    Price = 1000,
                    Type = "Apartment",
                    Images = new List<PropertyImageEntity>
                    {
                new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/apartment.jpg" }
                    }
                },
                new PropertyEntity
                {
                    Id = 22,
                    Title = "Old Villa",
                    OwnerId = 1,
                    IsAvailable = true,
                    IsDeleted = false,
                    Status = "For Sale",
                    Bedrooms = 5,
                    Bathrooms = 3,
                    City = "Hebron",
                    Price = 500000,
                    Type = "Villa",
                    Images = new List<PropertyImageEntity>
                    {
                new PropertyImageEntity { IsTheme = true, ImageUrl = "/images/villa.jpg" }
                    }
                }
            );

            _context.SaveChanges();

            // Set filters to only include properties in Ramallah with 2 bedrooms for rent
            var filters = new FiltersDto
            {
                City = "Ramallah",
                Bedrooms = 2,
                Status = "For Rent",
                PageNumber = 1,
                PageSize = 10
            };

            // Mock filter service to apply real filters on IQueryable
            var filterServiceMock = new Mock<IPropertyFilterService>();
            filterServiceMock
                .Setup(s => s.ApplyFilters(It.IsAny<IQueryable<PropertyEntity>>(), It.IsAny<FiltersDto>()))
                .Returns((IQueryable<PropertyEntity> source, FiltersDto f) =>
                {
                    var query = source;

                    if (!string.IsNullOrEmpty(f.City))
                        query = query.Where(p => p.City == f.City);

                    if (f.Bedrooms > 0)
                        query = query.Where(p => p.Bedrooms == f.Bedrooms);

                    if (!string.IsNullOrEmpty(f.Status))
                        query = query.Where(p => p.Status == f.Status);

                    return query;
                });

            var controller = new PropertyController(_context, filterServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // Act
            var result = controller.PublicSearch(filters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var response = Assert.IsType<FiltersResponseDto>(okResult.Value);

            // Only the "Modern Apartment" property matches filters
            Assert.Single(response.Properties);
            Assert.Equal(21, response.Properties[0].PropertyId);
            Assert.Equal("Modern Apartment", response.Properties[0].Title);
            Assert.Equal("Ramallah", response.Properties[0].City);
            Assert.Equal(2, response.Properties[0].Bedrooms);
            Assert.Equal("For Rent", response.Properties[0].Status);
        }


    }
}
