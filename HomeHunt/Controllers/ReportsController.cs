using HomeHunt.Constants;
using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HomeHunt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly HomeHuntDBContext _context;
        private readonly IEmailService _emailService;

        public ReportsController(HomeHuntDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost("scammer")]
        [Authorize]
        public async Task<IActionResult> SubmitReport([FromBody] ReportSubmissionDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int reporterId))
                return Unauthorized();

            var property = await _context.Properties
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == dto.PropertyId);

            if (property == null)
                return NotFound(new { error = "Property not found." });

            if (property.OwnerId == reporterId)
                return BadRequest(new { error = "You cannot report your own property." });

            bool alreadyReported = await _context.Reports
                .AnyAsync(r => r.PropertyId == dto.PropertyId && r.ReporterId == reporterId);

            if (alreadyReported)
                return BadRequest(new { error = "You have already reported this property." });

            // Add new report
            var report = new ReportEntity
            {
                ReporterId = reporterId,
                PropertyId = dto.PropertyId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            property.ReportCount++;

            var owner = property.Owner;

            // Check thresholds in descending order of importance
            if (property.ReportCount >= ReportThresholds.PropertyDeletionThreshold)
            {
                property.IsDeleted = true;
                owner.StrikeCount++;

                // Check for account deletion
                if (owner.StrikeCount >= ReportThresholds.OwnerStrikeLimit)
                {
                    var ownerProperties = await _context.Properties
                        .Where(p => p.OwnerId == owner.Id)
                        .ToListAsync();

                    foreach (var p in ownerProperties)
                    {
                        p.IsDeleted = true;
                    }

                    owner.IsActive = false;

                    await _emailService.SendAccountDeletedEmailAsync(owner.Email, owner.FirstName);
                }
                else
                {
                    // Notify: property deleted + strike
                    await _emailService.SendPropertyDeletedEmailAsync(owner.Email, property.Title, owner.FirstName);
                    await _emailService.SendAccountReportNotificationAsync(owner.Email, owner.FirstName);
                }
            }
            else if (property.ReportCount == ReportThresholds.WarningThreshold)
            {
                // Only send warning email
                await _emailService.SendWarningEmailAsync(owner.Email, property.Title, owner.FirstName);
            }
            else
            {
                // Send normal report email
                await _emailService.SendReportNotificationAsync(owner.Email, property.Title, owner.FirstName);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Report submitted successfully. We will review the property and take appropriate action."
            });
        }

    }
}