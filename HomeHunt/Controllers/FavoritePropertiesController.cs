using HomeHunt.Data;
using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeHunt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritePropertiesController : ControllerBase
    {
        private readonly HomeHuntDBContext _context;

        public FavoritePropertiesController(HomeHuntDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddFavorite([FromBody] FavoritePropertyDto request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId)) {
                return Unauthorized("Invalid user token.");
            }

            var property = await _context.Properties.FindAsync(request.PropertyId);
            if (property == null)
            {
                return NotFound("Property not found.");
            }

            var existingFavorite = await _context.UserFavoriteProperties
                            .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == request.PropertyId);
            if (existingFavorite != null)
            {
                return BadRequest("Property is already in your favorites.");
            }

            var favorite = new UserFavoriteProperties
            {
                UserId = userId,
                PropertyId = request.PropertyId
            };

            _context.UserFavoriteProperties.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Property added to favorites." });
        }

        [HttpDelete("favorite/{propertyId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFavorite(int propertyId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var favorite = await _context.UserFavoriteProperties
                .FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);
            if (favorite == null)
            {
                return NotFound("Property is not in your favorites.");
            }

            _context.UserFavoriteProperties.Remove(favorite);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Property removed from favorites." });
        }

        [HttpGet("favorite")]
        [Authorize]
        public async Task<IActionResult> GetFavorites()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid user token.");
            }

            var favorites = await _context.UserFavoriteProperties
                .Where(f => f.UserId == userId)
                .Include(f => f.Property)
                .Select(f => new
                {
                    propertyId = f.Property.Id,
                    city = f.Property.City,
                    price = f.Property.Price,
                    status = f.Property.Status,
                    rentDuration = f.Property.RentDuration,
                    bedrooms = f.Property.Bedrooms,
                    description = f.Property.Description,
                    isAvailable = f.Property.IsAvailable
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems = favorites.Count,
                properties = favorites
            });
        }
    }
}
