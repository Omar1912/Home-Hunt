using HomeHunt.Controllers;
using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore.InMemory;

namespace HomeHuntTests
{
    public class ReportsControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly HomeHuntDBContext _context;
        private readonly ReportsController _controller;

        public ReportsControllerTests()
        {
            var dbOptions = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _emailServiceMock = new Mock<IEmailService>();
            _context = new HomeHuntDBContext(dbOptions);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller = new ReportsController(_context, _emailServiceMock.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task SubmitReport_Valid_ShouldAddReport()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "Ali", Email = "ali@example.com" };
            var reporter = new UserEntity { Id = 1, FirstName = "Sara", Email = "sara@example.com" };
            var property = new PropertyEntity { Id = 10, Title = "Test Property", Owner = owner, OwnerId = owner.Id };

            _context.Users.AddRange(owner, reporter);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Single(_context.Reports);
            Assert.Equal(1, property.ReportCount);
        }

        [Fact]
        public async Task SubmitReport_SameUserCannotReportOwnProperty()
        {
            // Arrange
            var user = new UserEntity { Id = 1, FirstName = "Owner", Email = "owner@example.com" };
            var property = new PropertyEntity { Id = 20, Title = "Owner Property", Owner = user, OwnerId = user.Id };

            _context.Users.Add(user);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var error = badRequest.Value?.GetType().GetProperty("error")?.GetValue(badRequest.Value, null)?.ToString();
            Assert.Equal("You cannot report your own property.", error);
        }

        [Fact]
        public async Task SubmitReport_PropertyDoesNotExist()
        {
            // Arrange
            var user = new UserEntity { Id = 1, FirstName = "User", Email = "user@example.com" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = 999 };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var error = notFound.Value?.GetType().GetProperty("error")?.GetValue(notFound.Value, null)?.ToString();
            Assert.Equal("Property not found.", error);
        }


        [Fact]
        public async Task SubmitReport_AlreadyReported_ReturnsBadRequest()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "Owner", Email = "owner@example.com" };
            var reporter = new UserEntity { Id = 1, FirstName = "Reporter", Email = "rep@example.com" };
            var property = new PropertyEntity { Id = 30, Title = "Duplicate Test", Owner = owner, OwnerId = owner.Id };
            var report = new ReportEntity { ReporterId = reporter.Id, PropertyId = property.Id };

            _context.Users.AddRange(owner, reporter);
            _context.Properties.Add(property);
            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var resultText = badRequest.Value?.ToString();
            Assert.Contains("already reported", resultText);
        }

        [Fact]
        public async Task SubmitReport_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            var controller = new ReportsController(_context, _emailServiceMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // No user claims
            };

            var dto = new ReportSubmissionDTO { PropertyId = 1 };

            // Act
            var result = await controller.SubmitReport(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SubmitReport_TriggersWarningEmail_WhenThresholdReached()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "Ali", Email = "ali@example.com" };
            var reporter1 = new UserEntity { Id = 3, FirstName = "User1", Email = "u1@example.com" };
            var property = new PropertyEntity { Id = 40, Title = "Warning Threshold Property", Owner = owner, OwnerId = owner.Id, ReportCount = 1 };

            _context.Users.AddRange(owner, reporter1);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            _emailServiceMock.Verify(e =>
                e.SendWarningEmailAsync(owner.Email, property.Title, owner.FirstName), Times.Once);
        }

        [Fact]
        public async Task SubmitReport_TriggersDeletionEmail_WhenThresholdReached()
        {
            // Arrange
            var owner = new UserEntity
            {
                Id = 2,
                FirstName = "Ali",
                Email = "ali@example.com",
                StrikeCount = -1, 
                IsActive = true
            };

            var property = new PropertyEntity
            {
                Id = 50,
                Title = "Delete Me",
                Owner = owner,
                OwnerId = owner.Id,
                ReportCount = 2
            };

            _context.Users.Add(owner);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.True(property.IsDeleted);
            Assert.Equal(0, owner.StrikeCount);
            _emailServiceMock.Verify(e => e.SendPropertyDeletedEmailAsync(owner.Email, property.Title, owner.FirstName), Times.Once);
            _emailServiceMock.Verify(e => e.SendAccountReportNotificationAsync(owner.Email, owner.FirstName), Times.Once);
            _emailServiceMock.Verify(e => e.SendAccountDeletedEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }


        [Fact]
        public async Task SubmitReport_DeletesOwnerAccount_WhenStrikeLimitExceeded()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "Ali", Email = "ali@example.com", StrikeCount = 1, IsActive = true };
            var property = new PropertyEntity { Id = 60, Title = "Last Straw", Owner = owner, OwnerId = owner.Id, ReportCount = 2 };
            var extraProperty = new PropertyEntity { Id = 61, Title = "Another", Owner = owner, OwnerId = owner.Id, ReportCount = 0 };

            _context.Users.Add(owner);
            _context.Properties.AddRange(property, extraProperty);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.False(owner.IsActive);
            Assert.True(property.IsDeleted);
            Assert.True(extraProperty.IsDeleted);
            _emailServiceMock.Verify(e => e.SendAccountDeletedEmailAsync(owner.Email, owner.FirstName), Times.Once);
        }

        [Fact]
        public async Task SubmitReport_SendsReportNotificationEmail()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "Owner", Email = "owner@example.com" };
            var property = new PropertyEntity { Id = 70, Title = "Basic Report", Owner = owner, OwnerId = owner.Id };

            _context.Users.AddRange(owner);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            _emailServiceMock.Verify(e => e.SendReportNotificationAsync(owner.Email, property.Title, owner.FirstName), Times.Once);
        }

        [Fact]
        public async Task SubmitReport_SendsWarningEmail()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "WOwner", Email = "warn@example.com" };
            var property = new PropertyEntity
            {
                Id = 80,
                Title = "Warning Level",
                Owner = owner,
                OwnerId = owner.Id,
                ReportCount = 1
            };

            _context.Users.Add(owner);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            _emailServiceMock.Verify(e =>
                e.SendWarningEmailAsync(owner.Email, property.Title, owner.FirstName), Times.Once);
        }

        [Fact]
        public async Task SubmitReport_SendsDeletionAndStrikeEmails()
        {
            // Arrange
            var owner = new UserEntity
            {
                Id = 2,
                FirstName = "DelOwner",
                Email = "delete@example.com",
                StrikeCount = -1, 
                IsActive = true
            };

            var property = new PropertyEntity
            {
                Id = 90,
                Title = "Strike Me",
                Owner = owner,
                OwnerId = owner.Id,
                ReportCount = 2 
            };

            _context.Users.Add(owner);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            _emailServiceMock.Verify(e => e.SendPropertyDeletedEmailAsync(owner.Email, property.Title, owner.FirstName), Times.Once);
            _emailServiceMock.Verify(e => e.SendAccountReportNotificationAsync(owner.Email, owner.FirstName), Times.Once);
            _emailServiceMock.Verify(e => e.SendAccountDeletedEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }



        [Fact]
        public async Task SubmitReport_SendsAccountDeletedEmail()
        {
            // Arrange
            var owner = new UserEntity { Id = 2, FirstName = "BannedUser", Email = "banned@example.com", StrikeCount = 1 };
            var property = new PropertyEntity
            {
                Id = 100,
                Title = "Final Report",
                Owner = owner,
                OwnerId = owner.Id,
                ReportCount = 2
            };

            _context.Users.Add(owner);
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            var dto = new ReportSubmissionDTO { PropertyId = property.Id };

            // Act
            var result = await _controller.SubmitReport(dto);

            // Assert
            _emailServiceMock.Verify(e => e.SendAccountDeletedEmailAsync(owner.Email, owner.FirstName), Times.Once);
        }
    }
}
