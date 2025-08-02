using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace HomeHunt.Tests.Services
{
    public class UserServiceTests
    {
        private HomeHuntDBContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<HomeHuntDBContext>()
                .UseInMemoryDatabase(databaseName: "HomeHuntTestDB")
                .Options;

            var context = new HomeHuntDBContext(options);

            return context;
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenUserIsActiveAndExists()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            var activeUser = new UserEntity { Id = 1, IsActive = true };
            context.Users.Add(activeUser);
            await context.SaveChangesAsync();

            var userService = new UserService(context);

            // Act
            var result = await userService.GetUserByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserIsNotActive()
        {
            // Arrange
            var context = GetInMemoryDbContext();

            var inactiveUser = new UserEntity { Id = 2, IsActive = false };
            context.Users.Add(inactiveUser);
            await context.SaveChangesAsync();

            var userService = new UserService(context);

            // Act
            var result = await userService.GetUserByIdAsync(2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var userService = new UserService(context);

            // Act
            var result = await userService.GetUserByIdAsync(999); // non-existing ID

            // Assert
            Assert.Null(result);
        }
    }
}
