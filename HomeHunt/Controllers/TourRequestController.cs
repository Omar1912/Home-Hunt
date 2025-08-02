using HomeHunt.Models;
using HomeHunt.Services;
using HomeHunt.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HomeHunt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TourRequestController : ControllerBase
    {
        private readonly ITourRequestService _tourRequestService;
        private readonly IUserService _userService;
        private readonly IPropertyService _propertyService;
        private readonly IEmailService _emailService;

        public TourRequestController(
            ITourRequestService tourRequestService,
            IUserService userService,
            IEmailService emailService,
            IPropertyService propertyService)
        {
            _tourRequestService = tourRequestService;
            _userService = userService;
            _propertyService = propertyService;
            _emailService = emailService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTourRequest([FromBody] TourRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { Message = "Oops, looks like you're not logged in." });

                // Create the tour request
                var message = await _tourRequestService.CreateTourRequestAsync(userId, requestDto);

                // Send notification to property owner
                await NotifyOwnerAsync(userId, requestDto.PropertyId, requestDto);
                await NotifyRequesterAsync(userId, requestDto.PropertyId, requestDto);

                return Ok(new { Message = message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        private async Task NotifyOwnerAsync(int requesterId, int propertyId, TourRequestDTO requestDto)
        {
            var requester = await _userService.GetUserByIdAsync(requesterId);
            string requesterUsername = $"{requester?.FirstName} {requester?.LastName}".Trim();
            string requesterPhone = requester?.PhoneNumber ?? "Not provided";
            string requesterEmail = requester?.Email ?? "Not provided";

            var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(propertyId);
            if (ownerId == null) return;

            var owner = await _userService.GetUserByIdAsync(ownerId);
            var ownerEmail = owner?.Email;

            if (!string.IsNullOrWhiteSpace(ownerEmail))
            {
                await _emailService.SendTourRequestNotificationAsync(
                    ownerEmail,
                    requesterUsername,
                    requesterPhone,
                    requesterEmail,
                    requestDto // Send the full request details
         
                );
            }
        }
        private async Task NotifyRequesterAsync(int requesterId, int propertyId, TourRequestDTO requestDto)
        {
            var requester = await _userService.GetUserByIdAsync(requesterId);
            if (requester == null || string.IsNullOrWhiteSpace(requester.Email))
                return;

            var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(propertyId);
            if (ownerId == 0) // assuming ownerId is int, default is 0 if not found
                return;

            var owner = await _userService.GetUserByIdAsync(ownerId);
            if (owner == null)
                return;

            var property = await _propertyService.GetPropertyByIdAsync(propertyId);
            if (property == null)
                return;

            await _emailService.SendTourRequestConfirmationAsync(
                requester.Email,
                requestDto,
                owner,
                property);
        }

    }
}

