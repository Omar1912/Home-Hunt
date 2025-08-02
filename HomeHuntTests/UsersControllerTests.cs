using HomeHunt.Services.Interfaces;
using HomeHunt.Controllers;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;


namespace HomeHunt.UsersControllerTests
{
    public class UsersControllerTests
    {
        private readonly Mock<UserManager<UserEntity>> _userManagerMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly UsersController _controller;
        private readonly IConfiguration _config;


        public UsersControllerTests()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x["JwtConfig:Key"]).Returns("supersecretkey123supersecretkey123");
            configMock.Setup(x => x["JwtConfig:Issuer"]).Returns("issuer");
            configMock.Setup(x => x["JwtConfig:Audience"]).Returns("audience");
            _config = configMock.Object;

            var store = new Mock<IUserStore<UserEntity>>();
            _userManagerMock = new Mock<UserManager<UserEntity>>(store.Object, null, null, null, null, null, null, null, null); _jwtServiceMock = new Mock<IJwtService>();
            _emailServiceMock = new Mock<IEmailService>();
            _controller = new UsersController(_jwtServiceMock.Object, _userManagerMock.Object, _config);

            var claims = new Claim[] { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        private string GenerateTestToken(string email, string code)
        {
            var key = Encoding.UTF8.GetBytes(_config["JwtConfig:Key"] ?? "supersecretkey123supersecretkey123");
            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["JwtConfig:Issuer"] ?? "issuer",
                audience: _config["JwtConfig:Audience"] ?? "audience",
                claims: new[]
                {
            new Claim(ClaimTypes.Email, email),
            new Claim("verification_code", code),
            new Claim("purpose", "email_verification")
                },
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity)null);
            var dto = new LoginRequestDto { Email = "test@example.com", Password = "password" };
            var result = await _controller.Login(dto);
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid email or password.", unauthorizedResult.Value);
        }
        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            var user = new UserEntity { Id = 1, Email = "test@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, "password")).ReturnsAsync(true);
            _jwtServiceMock.Setup(x => x.GenerateJwtToken(user)).Returns("jwt-token");
            var dto = new LoginRequestDto { Email = "test@example.com", Password = "password" };
            var result = await _controller.Login(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            var tokenPop = response.GetType().GetProperty("Token");
            Assert.NotNull(tokenPop);
            var token = tokenPop.GetValue(response)?.ToString();
            Assert.Equal("jwt-token", token );
        }
        [Fact]
        public async Task SignUp_EmailExists_ReturnsBadRequest()
        {
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new UserEntity());
            var dto = new RegistrationDto
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                MobileNumber = "1234567890"
            };
            var result = await _controller.SignUp(dto, _emailServiceMock.Object);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var modelState = badRequestResult.Value as SerializableError;
            Assert.True(modelState.ContainsKey("Email"));
            Assert.Equal("This email address is used", ((string[])modelState["Email"])[0]);
        }
        [Fact]
        public async Task SignUp_ValidData_CreatesUserAndReturnsCreated()
        {
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<UserEntity>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            var dto = new RegistrationDto
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                MobileNumber = "1234567890"
            };
            var result = await _controller.SignUp(dto, _emailServiceMock.Object);
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            var idPop = createdResult.Value.GetType();
            Assert.NotNull(idPop);
            var idValue = idPop.GetProperty("userId").GetValue(createdResult.Value)?.ToString();
            Assert.NotNull(idValue);
           
        }
        [Fact]
        public async Task Delete_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange: Simulate no identity claims (unauthenticated)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.Delete();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorProp = unauthorizedResult.Value.GetType().GetProperty("error");
            var errorValue = errorProp?.GetValue(unauthorizedResult.Value)?.ToString();
            Assert.Equal("Invalid token.", errorValue);
        }
        [Fact]
        public async Task VerifyEmail_ValidToken_ReturnsOk()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";
            var token = GenerateTestToken(email, code);

            var dto = new EmailVerificationDto
            {
                Email = email,
                Code = code,
                Token = token
            };

            var user = new UserEntity { Email = email, EmailConfirmed = false };
            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.VerifyEmail(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var message = okResult.Value?.GetType().GetProperty("message")?.GetValue(okResult.Value)?.ToString();
            Assert.Equal("Email verified successfully.", message);
        }


        [Fact]
        public async Task VerifyEmail_MismatchedClaims_ReturnsBadRequest()
        {
            // Arrange
            var email = "test@example.com";
            var code = "wrongcode"; // mismatched on purpose

            var key = Encoding.UTF8.GetBytes(_config["JwtConfig:Key"]);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Email, email),
            new Claim("verification_code", "correctcode"),
            new Claim("purpose", "email_verification")
        }),
                Expires = DateTime.UtcNow.AddMinutes(10),
                Issuer = _config["JwtConfig:Issuer"],
                Audience = _config["JwtConfig:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var dto = new EmailVerificationDto
            {
                Email = email,
                Code = code,
                Token = tokenString
            };

            // Act
            var result = await _controller.VerifyEmail(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid verification data.", badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_UserNotFoundOrInactive_ReturnsNotFound()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync((UserEntity)null); // or inactive user
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Delete();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorProp = notFoundResult.Value.GetType().GetProperty("error");
            var errorValue = errorProp?.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("User not found.", errorValue);
        }
        [Fact]
        public async Task Delete_ValidUser_SoftDeletesAndReturnsNoContent()
        {
            // Arrange
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };

            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Delete();

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.False(user.IsActive); // confirm soft delete
        }
        [Fact]
        public async Task Get_UserDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync((UserEntity)null);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Get();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var type = response.GetType();
            var errorProp = type.GetProperty("error");
            Assert.NotNull(errorProp);
            var errorValue = errorProp.GetValue(response)?.ToString();
            Assert.Equal("User doesn't exist.", errorValue);
        }
        [Fact]
        public async Task Get_ValidUser_ReturnsUserDetails()
        {
            // Arrange
            var user = new UserEntity
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                MobileNumber = "1234567890",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);

            // Set up claims identity with NameIdentifier
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var type = response.GetType();

            Assert.Equal("test@example.com", type.GetProperty("Email")?.GetValue(response)?.ToString());
            Assert.Equal("Test", type.GetProperty("FirstName")?.GetValue(response)?.ToString());
            Assert.Equal("User", type.GetProperty("LastName")?.GetValue(response)?.ToString());
            Assert.Equal("1234567890", type.GetProperty("MobileNumber")?.GetValue(response)?.ToString());
        }
        [Fact]
        public async Task GetPublicProfile_ValidUser_ReturnsPublicProfile()
        {
            // Arrange
            int userId = 3;
            var user = new UserEntity
            {
                Id = userId,
                Email = "public@example.com",
                FirstName = "Public",
                LastName = "User",
                MobileNumber = "9998887777",
                IsActive = true
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                            .ReturnsAsync(user);

            // Act
            var result = await _controller.GetPublicProfile(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var type = response.GetType();

            Assert.Equal("public@example.com", type.GetProperty("Email")?.GetValue(response)?.ToString());
            Assert.Equal("Public", type.GetProperty("FirstName")?.GetValue(response)?.ToString());
            Assert.Equal("User", type.GetProperty("LastName")?.GetValue(response)?.ToString());
            Assert.Equal("9998887777", type.GetProperty("MobileNumber")?.GetValue(response)?.ToString());
        }

        [Fact]
        public async Task GetPublicProfile_UserIsNotActive_ReturnsBadRequest()
        {
            // Arrange
            int userId = 2;
            var user = new UserEntity
            {
                Id = userId,
                IsActive = false
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                            .ReturnsAsync(user);

            // Act
            var result = await _controller.GetPublicProfile(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var type = response.GetType();
            var errorProp = type.GetProperty("error");
            Assert.NotNull(errorProp);
            var errorValue = errorProp.GetValue(response)?.ToString();
            Assert.Equal("User doesn't exist.", errorValue);
        }
        [Fact]
        public async Task GetPublicProfile_UserDoesNotExist_ReturnsBadRequest()
    {
        // Arrange
        int userId = 1;
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                        .ReturnsAsync((UserEntity)null);

        // Act
        var result = await _controller.GetPublicProfile(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = badRequestResult.Value;
        var type = response.GetType();
        var errorProp = type.GetProperty("error");
        Assert.NotNull(errorProp);
        var errorValue = errorProp.GetValue(response)?.ToString();
        Assert.Equal("User doesn't exist.", errorValue);
    }

        [Fact]
        public async Task ChangePassword_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange: Simulate missing identity
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPass123!",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var errorProp = unauthorizedResult.Value.GetType().GetProperty("error");
            var error = errorProp?.GetValue(unauthorizedResult.Value)?.ToString();
            Assert.Equal("Invalid token.", error);
        }

        [Fact]
        public async Task ChangePassword_UserNotFoundOrInactive_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync((UserEntity)null); // or inactive user

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPass123!",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var errorProp = notFoundResult.Value.GetType().GetProperty("error");
            var error = errorProp?.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("User not found.", error);
        }

        [Fact]
        public async Task ChangePassword_ValidUser_ChangesPasswordAndReturnsOk()
        {
            // Arrange
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };

            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, "OldPass123!", "NewPass123!"))
                .ReturnsAsync(IdentityResult.Success);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPass123!",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Password has been changed. ", okResult.Value);
        }

        [Fact]
        public async Task ForgotPassword_UserDoesNotExist_ReturnsOkWithGenericMessage()
        {
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity)null);
            var dto = new ForgotPasswordDTO { Email = "nonexistent@example.com" };
            var result = await _controller.ForgotPassword(dto, _emailServiceMock.Object);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var propertyInfo = okResult.Value?.GetType().GetProperty("message");
            Assert.NotNull(propertyInfo);  // Helps identify if the property was missing
            var okSentence = propertyInfo.GetValue(okResult.Value)?.ToString();
            Assert.Equal("If the email exists, a reset link has been sent.", okSentence);


        }
        [Fact]
        public async Task ForgotPassword_UserExists_SendsEmailAndReturnsOk()
        {
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };
            _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            _emailServiceMock.Setup(x => x.SendPasswordResetEmailAsync("test@example.com", "reset-token")).Returns(Task.CompletedTask);
            var dto = new ForgotPasswordDTO { Email = "test@example.com" };
            var result = await _controller.ForgotPassword(dto, _emailServiceMock.Object);
            var okResult = Assert.IsType<OkObjectResult>(result);

            var responseType = okResult.Value.GetType();
            var messageProp = responseType.GetProperty("message");
            var tokenProp = responseType.GetProperty("token");

            Assert.NotNull(messageProp);
            Assert.NotNull(tokenProp);

            var message = messageProp.GetValue(okResult.Value)?.ToString();
            var token = tokenProp.GetValue(okResult.Value)?.ToString();

            Assert.Equal("If the email exists, a reset link has been sent.", message);
            Assert.Equal("reset-token", token);

            _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync("test@example.com", "reset-token"), Times.Once());
        }
        [Fact]
        public async Task ResetPassword_UserDoesNotExist_ReturnsBadRequest()
        {
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity)null);
            var dto = new ResetPasswordDTO
            {
                Email = "nonexistent@example.com",
                Token = "token",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };
            var result = await _controller.ResetPassword(dto);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);

            var response = badRequestResult.Value.GetType();
            var errorpop = response.GetProperty("error");
            Assert.NotNull(errorpop);
            var error = errorpop.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("Invalid or inactive user.", error);
        }
        [Fact]
        public async Task ResetPassword_ValidToken_ResetsPasswordAndReturnsOk()
        {
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };
            _userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "valid-token", "NewPass123!")).ReturnsAsync(IdentityResult.Success);
            var dto = new ResetPasswordDTO
            {
                Email = "test@example.com",
                Token = "valid-token",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };
            var result = await _controller.ResetPassword(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value.GetType();
            var okMessagePop = response.GetProperty("message");
            var message = okMessagePop.GetValue(okResult.Value)?.ToString();
            Assert.Equal("Password reset successfully.",message);

        }
        // Tests for UpdateProfile method
        [Fact]
        public async Task UpdateProfile_UnauthorizedUser_ReturnsUnauthorized()
        {
            // Arrange: Simulate an invalid token
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            var dto = new UpdateProfileDTO
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                MobileNumber = "9876543210"
            };

            // Act
            var result = await _controller.UpdateProfile(dto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var response = unauthorizedResult.Value.GetType().GetProperty("error");
            var error = response?.GetValue(unauthorizedResult.Value)?.ToString();
            Assert.Equal("Invalid token.", error);
        }
        [Fact]
        public async Task UpdateProfile_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync((UserEntity)null);

            var dto = new UpdateProfileDTO
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                MobileNumber = "9876543210"
            };

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.UpdateProfile(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value.GetType().GetProperty("error");
            Assert.NotNull(response);
            var error = response.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("User is not available!", error);
        }
        [Fact]
        public async Task UpdateProfile_UserInactive_ReturnsNotFound()
        {
            // Arrange
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = false };
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);

            var dto = new UpdateProfileDTO
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                MobileNumber = "9876543210"
            };

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.UpdateProfile(dto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = notFoundResult.Value.GetType().GetProperty("error");
            Assert.NotNull(response);
            var error = response.GetValue(notFoundResult.Value)?.ToString();
            Assert.Equal("User is not available!", error);
        }
        [Fact]
        public async Task UpdateProfile_UpdateFails_ReturnsBadRequest()
        {
            // Arrange
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Validation error occurred." }));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var dto = new UpdateProfileDTO
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                MobileNumber = "9876543210"
            };

            // Act
            var result = await _controller.UpdateProfile(dto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorProp = badRequestResult.Value.GetType().GetProperty("error");
            var error = errorProp?.GetValue(badRequestResult.Value)?.ToString();
            Assert.Equal("Faild to update profile.", error);
        }
        [Fact]
        public async Task UpdateProfile_SuccessfulUpdate_ReturnsOk()
        {
            // Arrange
            var user = new UserEntity { Id = 1, Email = "test@example.com", IsActive = true };
            _userManagerMock.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var dto = new UpdateProfileDTO
            {
                FirstName = "NewFirstName",
                LastName = "NewLastName",
                MobileNumber = "9876543210"
            };

            // Act
            var result = await _controller.UpdateProfile(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            Assert.NotNull(response);
            var type = response.GetType();

            Assert.Equal("Profile updated successfully. ", type.GetProperty("message")?.GetValue(response)?.ToString());
            Assert.Equal("NewFirstName", type.GetProperty("firstName")?.GetValue(response)?.ToString());
            Assert.Equal("NewLastName", type.GetProperty("lastName")?.GetValue(response)?.ToString());
            Assert.Equal("9876543210", type.GetProperty("mobileNumber")?.GetValue(response)?.ToString());

            // Confirm user object was updated
            Assert.Equal("NewFirstName", user.FirstName);
            Assert.Equal("NewLastName", user.LastName);
            Assert.Equal("9876543210", user.MobileNumber);
        }

    }
}