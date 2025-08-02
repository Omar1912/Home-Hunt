using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class TourRequestService : ITourRequestService
{
    private readonly HomeHuntDBContext _context;
    private readonly IUserService _userService;
    private readonly IPropertyService _propertyService;

    public TourRequestService(HomeHuntDBContext context, IUserService userService, IPropertyService propertyService)
    {
        _context = context;
        _userService = userService;
        _propertyService = propertyService;
    }

    public async Task<string> CreateTourRequestAsync(int userId, TourRequestDTO requestDto)
    {
        // Full validation happens here
        await ValidateTourRequestAsync(userId, requestDto);

        var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(requestDto.PropertyId);

        var tour = new TourEntity
        {
            PropertyId = requestDto.PropertyId,
            UserId = userId,
            OwnerId = ownerId,
            PreferredDate1 = ParseDate(requestDto.PreferredDate1),
            PreferredDate2 = ParseDate(requestDto.PreferredDate2),
            PreferredDate3 = ParseDate(requestDto.PreferredDate3),
            Notes = requestDto.Notes ?? string.Empty,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.TourRequests.Add(tour);
        await _context.SaveChangesAsync();

        return "You have successfully requested a tour.";
    }

    public async Task ValidateTourRequestAsync(int userId, TourRequestDTO requestDto)
    {
        // Check user
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || !user.IsActive)
            throw new ArgumentException("Your account is not active or does not exist.");

        // Property
        if (!await _propertyService.PropertyExistsAsync(requestDto.PropertyId))
            throw new ArgumentException("Property not found.");
 
        var ownerId = await _propertyService.GetOwnerIdByPropertyIdAsync(requestDto.PropertyId);
        if (ownerId == 0)
            throw new ArgumentException("Owner not found for the specified property.");

        // Owner active?
        var owner = await _userService.GetUserByIdAsync(ownerId);
        if (owner == null || !owner.IsActive)
            throw new ArgumentException("Property owner is not active.");

        // Existing tour?
        if (await UserHasTourRequest(userId, requestDto.PropertyId))
            throw new ArgumentException("You already have a tour request for this property.");

        // Date parsing & validation
        var date1 = ParseDate(requestDto.PreferredDate1);
        var date2 = ParseDate(requestDto.PreferredDate2);
        var date3 = ParseDate(requestDto.PreferredDate3);

        var validationMessage = ValidateDates(date1, date2, date3);
        if (!string.IsNullOrEmpty(validationMessage))
            throw new ArgumentException(validationMessage);
    }

    private DateTime? ParseDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (DateTime.TryParse(input, out var result))
            return result;

        throw new ArgumentException("Invalid date format.");
    }

    private string ValidateDates(DateTime? date1, DateTime? date2, DateTime? date3)
    {
        var now = DateTime.UtcNow;

        if ((date1.HasValue && date1 < now) ||
            (date2.HasValue && date2 < now) ||
            (date3.HasValue && date3 < now))
            return "Preferred dates must be in the future.";

        var dates = new List<DateTime?> { date1, date2, date3 }
            .Where(d => d.HasValue)
            .Select(d => d.Value)
            .ToList();

        if (dates.Count != dates.Distinct().Count())
            return "Duplicate dates are not allowed.";

        return string.Empty;
    }

    private async Task<bool> UserHasTourRequest(int userId, int propertyId)
    {
        return await _context.TourRequests.AnyAsync(tr => tr.UserId == userId && tr.PropertyId == propertyId);
    }
}

