using HomeHunt.Controllers;
using HomeHunt.Models;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System;

namespace HomeHuntTests_TourRequest
{
    public class TourRequestControllerTests
    {
        private readonly Mock<ITourRequestService> _mockTourRequestService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IPropertyService> _mockPropertyService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly TourRequestController _controller;

        public TourRequestControllerTests()
        {
            _mockTourRequestService = new Mock<ITourRequestService>();
            _mockUserService = new Mock<IUserService>();
            _mockPropertyService = new Mock<IPropertyService>();
            _mockEmailService = new Mock<IEmailService>();

            _controller = new TourRequestController(
                _mockTourRequestService.Object,
                _mockUserService.Object,
                _mockEmailService.Object,
                _mockPropertyService.Object
            );

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task CreateTourRequest_ValidRequest_ReturnsOk()
        {
            var dto = new TourRequestDTO
            {
                PropertyId = 1,
                PreferredDate1 = "2025-06-18",
                PreferredDate2 = "2025-06-19",
                Notes = "Looking forward to the visit"
            };

            var requester = new UserEntity { Id = 1, FirstName = "Falasteen", LastName = "Abu Ali", PhoneNumber = "123456789", Email = "falasteenabuali@gmail.com" };
            var owner = new UserEntity { Id = 2, FirstName = "Owner", LastName = "User", PhoneNumber = "987654321", Email = "1210661@student.birzeit.edu" };
            var property = new PropertyEntity { Id = 1, Title = "Cozy Apartment", City = "Ramallah", Village = "Al-Masyoun", Description = "Nice view and spacious" };

            _mockTourRequestService.Setup(s => s.CreateTourRequestAsync(1, dto)).ReturnsAsync("Tour request created successfully");
            _mockUserService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(requester);
            _mockPropertyService.Setup(s => s.GetOwnerIdByPropertyIdAsync(1)).ReturnsAsync(2);
            _mockUserService.Setup(s => s.GetUserByIdAsync(2)).ReturnsAsync(owner);
            _mockPropertyService.Setup(s => s.GetPropertyByIdAsync(1)).ReturnsAsync(property);
            _mockEmailService.Setup(e => e.SendTourRequestNotificationAsync(owner.Email, It.IsAny<string>(), It.IsAny<string>(), requester.Email, dto)).Returns(Task.CompletedTask);
            _mockEmailService.Setup(e => e.SendTourRequestConfirmationAsync(requester.Email, dto, owner, property)).Returns(Task.CompletedTask);

            var result = await _controller.CreateTourRequest(dto);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = okResult.Value;
            var messageProp = resultValue.GetType().GetProperty("Message");
            Assert.NotNull(messageProp);
            var actualMessage = messageProp.GetValue(resultValue) as string;
            Assert.Equal("Tour request created successfully", actualMessage);
        }

        [Fact]
        public async Task CreateTourRequest_InvalidModel_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("PropertyId", "Required");

            var dto = new TourRequestDTO();

            var result = await _controller.CreateTourRequest(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateTourRequest_UnauthenticatedUser_ReturnsUnauthorized()
        {
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();

            var dto = new TourRequestDTO { PropertyId = 1 };
            var result = await _controller.CreateTourRequest(dto);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var messageProp = unauthorizedResult.Value.GetType().GetProperty("Message");
            Assert.NotNull(messageProp);
            var actualMessage = messageProp.GetValue(unauthorizedResult.Value) as string;
            Assert.Equal("Oops, looks like you're not logged in.", actualMessage);
        }

        [Fact]
        public async Task CreateTourRequest_ThrowsArgumentException_ReturnsBadRequest()
        {
            var dto = new TourRequestDTO { PropertyId = 1 };

            _mockTourRequestService
                .Setup(s => s.CreateTourRequestAsync(1, dto))
                .ThrowsAsync(new ArgumentException("Invalid property ID"));

            var result = await _controller.CreateTourRequest(dto);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var messageProp = badRequestResult.Value.GetType().GetProperty("Message");
            Assert.NotNull(messageProp);
            var actualMessage = messageProp.GetValue(badRequestResult.Value) as string;
            Assert.Equal("Invalid property ID", actualMessage);
        }

        [Fact]
        public async Task CreateTourRequest_ThrowsException_ReturnsServerError()
        {
            var dto = new TourRequestDTO { PropertyId = 1 };

            _mockTourRequestService
                .Setup(s => s.CreateTourRequestAsync(1, dto))
                .ThrowsAsync(new Exception("Database failure"));

            var result = await _controller.CreateTourRequest(dto);

            var errorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, errorResult.StatusCode);
            var messageProp = errorResult.Value.GetType().GetProperty("Message");
            Assert.NotNull(messageProp);
            var actualMessage = messageProp.GetValue(errorResult.Value) as string;
            Assert.Contains("Database failure", actualMessage);
        }
    }
}
