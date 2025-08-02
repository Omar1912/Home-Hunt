using HomeHunt.Data;
using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly HomeHuntDBContext _context;

    public LocationController(HomeHuntDBContext context)
    {
        _context = context;
    }

    // GET: api/location/cities?search=term
    [HttpGet("cities")]
    public IActionResult GetCities(string? search)
    {
        var query = _context.Cities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.City.Contains(search));
        }

        var cities = query
            .Select(c => new { id = c.Id, name = c.City })
            .ToList();

        return Ok(cities);
    }

    [HttpGet("villages")]
    public IActionResult GetVillages(int? cityId, string? search)
    {
        IQueryable<VillageEntity> query = _context.Villages;

        if (cityId.HasValue && cityId.Value > 0)
        {
            query = query.Where(v => v.CityId == cityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.Name.Contains(search));
        }

        var villages = query
            .Select(v => new { id = v.Id, name = v.Name })
            .ToList();

        return Ok(villages);
    }
}