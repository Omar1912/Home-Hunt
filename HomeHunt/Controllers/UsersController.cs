using HomeHunt.Models;
using Microsoft.AspNetCore.Mvc;
using HomeHunt.Services.Interfaces;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Identity;
using HomeHunt.Data;
using HomeHunt.Controllers;
using HomeHunt.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace HomeHunt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IJwtService jwtService;
        private readonly UserManager<UserEntity> userManager;
        private readonly IConfiguration _config;
        public UsersController(IJwtService jwtService, UserManager<UserEntity> userManager, IConfiguration config)
        {
            this.jwtService = jwtService;
            this.userManager = userManager; // Inject UserManager 
            _config = config;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and password are required.");
            }

            // Find the user by email
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive|| !user.EmailConfirmed)
            {
                return Unauthorized("Invalid email or password.");
            }

            // Check password manually
            var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid email or password.");
            }

            // Generate JWT token
            var token = jwtService.GenerateJwtToken(user);
            return Ok(new { Token = token });
        }


        [HttpPost("Signup")]
        public async Task<IActionResult> SignUp([FromBody] RegistrationDto registration, [FromServices] IEmailService emailService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await userManager.FindByEmailAsync(registration.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email address is used");
                return BadRequest(ModelState);
            }

            var newUser = new UserEntity()

            {
                Email = registration.Email,
                UserName = registration.Email,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                MobileNumber = registration.MobileNumber,
            };
            var code = new Random().Next(100000, 999999).ToString();
            var token = jwtService.GenerateEmailVerificationToken(newUser.Email, code);

            await emailService.SendVerificationCodeAsync(newUser.Email, code);
            var result = await userManager.CreateAsync(newUser, registration.Password);

            if (result.Succeeded)
            {
                return StatusCode(201, new {
                    userId = newUser.Id,
                    token = token
                });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }


        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDto modle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["JwtConfig:Key"]);

            try
            {
                tokenHandler.ValidateToken(modle.Token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _config["JwtConfig:Issuer"],
                    ValidAudience = _config["JwtConfig:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var emailClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
                var codeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "verification_code")?.Value;
                var purposeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value;

                if (emailClaim != modle.Email || codeClaim != modle.Code || purposeClaim != "email_verification")
                {
                    return BadRequest("Invalid verification data.");
                }

                var user = await userManager.FindByEmailAsync(modle.Email);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }

                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                return Ok(new { message = "Email verified successfully." });
            }

            catch (SecurityTokenException)
            {
                return BadRequest("Invalid or expired token.");
            }
        }


        [HttpDelete("DeleteProfile")]
        [Authorize]
        public async Task<IActionResult> Delete()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Unauthorized(new { error = "Invalid token." });
            }
            ;

            var user = await userManager.FindByIdAsync(authenticatedUserId.ToString());

            if (user == null || !user.IsActive)
            {
                return NotFound(new { error = "User not found." });
            }

            user.IsActive = false;
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { error = "Failed to deactivate user.", details = errors });
            }

            return NoContent();
        }


        [HttpGet("UserProfile")]
        public async Task<IActionResult> Get()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Unauthorized(new { error = "Invalid token." });
            }

            var user = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            if (user == null || !user.IsActive)
            {
                return BadRequest(new { error = "User doesn't exist." });
            }

            return Ok(new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.MobileNumber
            });
        }

        [HttpGet("PublicUserProfile/{id}")]
        public async Task<IActionResult> GetPublicProfile(int id)
        {
            var user = await userManager.FindByIdAsync(id.ToString());
            if (user == null || !user.IsActive)
            {
                return BadRequest(new { error = "User doesn't exist." });
            }

            return Ok(new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.MobileNumber
            });
        }

        [HttpPatch("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Unauthorized(new { error = "Invalid token." });
            }
            var user = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            if (user == null || !user.IsActive)
            {
                return NotFound(new { error = "User not found." });
            }
            var reuslt = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!reuslt.Succeeded)
            {
                return BadRequest(reuslt.Errors);
            }
            return Ok("Password has been changed. ");
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model, [FromServices] IEmailService emailService)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return Ok(new { message = "If the email exists, a reset link has been sent." });
            }

            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailService.SendPasswordResetEmailAsync(model.Email, token);

            return Ok(new
            {
                message = "If the email exists, a reset link has been sent.",
            });
        }

        [HttpPatch("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return BadRequest(new { error = "Invalid or inactive user." });
            }
            var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { error = "Failed to reset password.", details = errors });
            }
            return Ok(new { message = "Password reset successfully." });
        }

        [HttpPatch("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var authenticatedUserId))
            {
                return Unauthorized(new { error = "Invalid token." });
            }
            var user = await userManager.FindByIdAsync(authenticatedUserId.ToString());
            if (user == null || !user.IsActive)
            {
                return NotFound(new { error = "User is not available!" });
            }
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.MobileNumber = model.MobileNumber;
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { error = "Faild to update profile.", details = errors });
            }
            return Ok(new
            {
                message = "Profile updated successfully. ",
                firstName = model.FirstName,
                lastName = model.LastName,
                mobileNumber = model.MobileNumber
            });
        }
    }
}