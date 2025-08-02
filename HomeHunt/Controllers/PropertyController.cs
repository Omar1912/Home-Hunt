using Microsoft.AspNetCore.Mvc;
using System.Linq;
using HomeHunt.Data;
using HomeHunt.Models.Entities;
using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HomeHunt.Services.Filters;
using HomeHunt.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json.Serialization;


namespace HomeHunt.Controllers
{
    [Route("api/properties")]
    [ApiController]
    public class PropertyController : ControllerBase
    {
        private readonly HomeHuntDBContext _context;
        private readonly IPropertyFilterService _filterService;

        public PropertyController(HomeHuntDBContext context, IPropertyFilterService filterService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        }

        // ========== FILTERED SEARCH ==========
        [AllowAnonymous]
        [HttpGet("search/public")]
        public IActionResult PublicSearch([FromQuery] FiltersDto filters_for_public)
        {
            if (filters_for_public == null)
                return BadRequest("Filter parameters are required.");

            var validationResult = ValidateFilterInput(filters_for_public);
            if (validationResult != null)
                return validationResult;

            // Single query combining filters and active owner check
            var query = from p in _filterService.ApplyFilters(
                            _context.Properties.Where(p => p.IsAvailable && !p.IsDeleted),
                            filters_for_public)
                        join u in _context.Users on p.OwnerId equals u.Id
                        where u.IsActive
                        select p;

 

           return ArrangeFiltersResponse(query, filters_for_public);
        }




        [Authorize]
        [HttpGet("search/private")]
        public IActionResult PrivatePropertiesSearch([FromQuery] PrivateFiltersDTO filters_for_private)
        {

            if (filters_for_private == null)
                return BadRequest("Filter parameters are required.");

            var validationResult = ValidateFilterInput(filters_for_private);
            if (validationResult != null)
                return validationResult;
            var ownerId = GetUserIdFromToken();

            if (ownerId == null)
            {
                return Unauthorized(new { success = false, message = "User ID not found in token." });
            }
            // Single query combining filters and active owner check
            var query = from p in _filterService.ApplyFilters(
                                    _context.Properties.Where(p => p.OwnerId == ownerId && p.IsAvailable && !p.IsDeleted),
                                    filters_for_private)
                        join u in _context.Users on p.OwnerId equals u.Id
                        where u.IsActive
                        select p;

            return ArrangeFiltersResponse(query, filters_for_private);


        }



        [HttpPost]
        [Route("add")]
        [Authorize]
        public async Task<IActionResult> AddProperty([FromForm] AddPropertyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .Select(ms => new
                    {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    });

                return BadRequest(new { Errors = allErrors });
            }

            if (dto.ImageFiles == null || !dto.ImageFiles.Any())
            {
                return BadRequest(new { error = "At least one image is required." });
            }

            try
            {
                var ownerClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (ownerClaim == null)
                {
                    return Unauthorized(new { error = "User ID claim not found in token." });
                }

                int ownerId = int.Parse(ownerClaim.Value);

                var property = new PropertyEntity
                {
                    OwnerId = ownerId,
                    City = dto.City,
                    Type = dto.Type,
                    Price = dto.Price,
                    Status = dto.Status,
                    Description = dto.Description,
                    Village = dto.Village,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    Utilities = dto.Utilities,
                    AvailabilityDate = dto.AvailabilityDate,
                    Policies = dto.Policies,
                    Requirements = dto.Requirements,
                    RentDuration = dto.RentDuration,
                    Bedrooms = dto.Bedrooms,
                    Title = dto.Title,
                    Street = dto.Street,
                    Kitchens = dto.Kitchens,
                    Bathrooms = dto.Bathrooms,
                    LivingRooms = dto.LivingRooms,
                    IsAvailable = true,
                    IsDeleted = false
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync(); // Save to get the property ID

                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                bool isFirst = true;

                foreach (var file in dto.ImageFiles)
                {
                    if (file.Length == 0)
                        continue;

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.PropertyImages.Add(new PropertyImageEntity
                    {
                        PropertyId = property.Id,
                        ImageUrl = $"/images/{fileName}",
                        IsTheme = isFirst
                    });
                    isFirst = false;
                    // Only the first image is marked as theme
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    propertyId = property.Id,
                    message = "Property added successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }

        // ========== DELETE PROPERTY (Soft Delete) ==========
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                    return NotFound(new { message = $"Property with ID {id} not found." });

                var ownerClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (ownerClaim == null || !int.TryParse(ownerClaim.Value, out int userId))
                    return Unauthorized(new { error = "Invalid or missing user claim." });

                if (property.OwnerId != userId)
                    return StatusCode(403, new { error = "You are not authorized to delete this property." });


                // Soft delete by setting IsDeleted = true
                property.IsDeleted = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Property with ID {id} marked as deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Delete failed.",
                    details = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("update/{id}")]
        [Authorize]
         
        public async Task<IActionResult> UpdateProperty(int id, [FromForm] UpdatePropertyDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var allErrors = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .Select(ms => new
                    {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    });

                return BadRequest(new { Errors = allErrors });
            }

            try
            {
                var property = await _context.Properties
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null || property.IsDeleted)
                    return NotFound(new { message = $"Property with ID {id} not found or has been deleted." });

                var ownerClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (ownerClaim == null || !int.TryParse(ownerClaim.Value, out int userId))
                    return Unauthorized(new { error = "Invalid or missing user claim." });

                if (property.OwnerId != userId)
                    return StatusCode(403, new { error = "You are not authorized to update this property." });

                // ========== Update Property Fields ==========
                property.City = dto.City;
                property.Type = dto.Type;
                property.Price = dto.Price;
                property.Status = dto.Status;
                property.Description = dto.Description;
                property.Village = dto.Village;
                property.Longitude = dto.Longitude;
                property.Latitude = dto.Latitude;
                property.Utilities = dto.Utilities;
                property.AvailabilityDate = dto.AvailabilityDate;
                property.Policies = dto.Policies;
                property.Requirements = dto.Requirements;
                property.RentDuration = dto.RentDuration;
                property.Bedrooms = dto.Bedrooms;
                property.Title = dto.Title;
                property.Street = dto.Street;
                property.Kitchens = dto.Kitchens;
                property.Bathrooms = dto.Bathrooms;
                property.LivingRooms = dto.LivingRooms;
                property.IsAvailable = dto.IsAvailable;

                // ========== Handle Property Images ==========
                // Step 1: Remove all existing image mappings
                var existingImages = await _context.PropertyImages
                    .Where(img => img.PropertyId == property.Id)
                    .ToListAsync();

                _context.PropertyImages.RemoveRange(existingImages);

                // Step 2: Re-add ImageUrlsToKeep (frontend assures they're valid)
                if (dto.ImageUrlsToKeep != null && dto.ImageUrlsToKeep.Any())
                {
                    foreach (var imageUrl in dto.ImageUrlsToKeep)
                    {
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            _context.PropertyImages.Add(new PropertyImageEntity
                            {
                                PropertyId = property.Id,
                                ImageUrl = imageUrl.Trim(),
                                IsTheme = false
                            });
                        }
                    }
                }

                // Step 3: Upload and add new image files
                if (dto.NewImageFiles != null && dto.NewImageFiles.Any())
                {
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);

                    foreach (var file in dto.NewImageFiles)
                    {
                        if (file.Length == 0) continue;

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        _context.PropertyImages.Add(new PropertyImageEntity
                        {
                            PropertyId = property.Id,
                            ImageUrl = $"/images/{fileName}",
                            IsTheme = false
                        });
                    }
                }

                // Save all changes to DB
                await _context.SaveChangesAsync();

                // Step 4: Ensure at least one image is marked as theme
                var updatedImages = await _context.PropertyImages
                    .Where(img => img.PropertyId == property.Id)
                    .ToListAsync();

                if (!updatedImages.Any(img => img.IsTheme))
                {
                    var first = updatedImages.FirstOrDefault();
                    if (first != null)
                    {
                        first.IsTheme = true;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = $"Property with ID {id} updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Update failed.",
                    details = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }


        // ========== FILTER VALIDATION HELPERS ==========
        private IActionResult? ValidateFilterInput(FiltersDto filters)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (filters.MinPrice < 0 || filters.MaxPrice < 0)
                return BadRequest("Price cannot be negative.");

            if (filters.Bedrooms < 0)
                return BadRequest("Bedrooms cannot be negative.");

            if (filters.Bathrooms < 0)
                return BadRequest("Bathrooms cannot be negative.");

            if (filters.kitchens < 0)
                return BadRequest("kitchens cannot be negative.");

            if (filters.LivingRooms < 0)
                return BadRequest("living rooms  cannot be negative.");

            if (filters.MinPrice.HasValue && filters.MaxPrice.HasValue &&
                filters.MinPrice > filters.MaxPrice)
                return BadRequest("MinPrice cannot be greater than MaxPrice.");

            var statusValidation = ValidateStatusAndRentDuration(filters);
            if (statusValidation != null) return statusValidation;

            if (filters.PageNumber <= 0)
                return BadRequest("PageNumber must be greater than 0.");

            if (filters.PageSize <= 0)
                return BadRequest("PageSize must be greater than 0.");

            var validTypes = new[] { "apartment", "house", "villa", "land", "commercial" };

            if (filters == null)
                return BadRequest("Filter parameters are required.");

            if (!string.IsNullOrWhiteSpace(filters.HomeType) &&
                !validTypes.Contains(filters.HomeType?.Trim().ToLowerInvariant()))
            {
                return BadRequest("Sorry, we don't have any properties of the type you searched for.");
            }

        
            return null;
        }

        private IActionResult? ValidateStatusAndRentDuration(FiltersDto filters)
        {
            var validStatuses = new[] { "for rent", "for sale" };
            if (!string.IsNullOrEmpty(filters.Status) &&
                !validStatuses.Contains(filters.Status.ToLowerInvariant()))
                return BadRequest("Invalid Status. It must be either 'for rent' or 'for sale'.");

            if (filters.Status?.ToLowerInvariant() == "for rent")
            {
                var validDurations = new[] { "monthly", "annual", "weekly" };
                if (!string.IsNullOrEmpty(filters.RentDuration) &&
                    !validDurations.Contains(filters.RentDuration.ToLowerInvariant()))
                    return BadRequest("Invalid Rent Duration. It must be either 'Monthly' or 'Annual' or Weekly.");
            }
            else if (!string.IsNullOrEmpty(filters.RentDuration))
            {
                return BadRequest("Rent Duration should only be provided for properties that are for rent.");
            }

            return null;
        }

        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null; // invalid or missing userId claim
        }

        private IActionResult ArrangeFiltersResponse(
        IQueryable<PropertyEntity> filteredQuery,
        FiltersDto filters)
        {
            try
            {
                int totalItems = filteredQuery.Count();

                if (totalItems == 0)
                    return NotFound("No properties found matching the specified filters.");

                int totalPages = (int)Math.Ceiling(totalItems / (double)filters.PageSize);
                int skip = (filters.PageNumber - 1) * filters.PageSize;
                // Get current user ID from token (if logged in)
                int? currentUserId = GetUserIdFromToken();
                // Get IDs of favorited properties (empty list if not logged in)
                var userFavoriteIds = currentUserId != null
                    ? _context.UserFavoriteProperties
                        .Where(fav => fav.UserId == currentUserId)
                        .Select(fav => fav.PropertyId)
                        .ToHashSet()
                    : new HashSet<int>();

                if (filters is PrivateFiltersDTO privateFilters)
                {
                    var pagedProperties = filteredQuery
                        .Skip(skip)
                        .Take(filters.PageSize)
                        .Select(p => new PrivatePropertyResponseDto
                        {
                            PropertyId = p.Id,
                            Title = p.Title,
                            City = p.City,
                            Village = p.Village,
                            Type = p.Type,
                            Price = p.Price,
                            Status = p.Status,
                            RentDuration = p.RentDuration,
                            Bedrooms = p.Bedrooms,
                            Kitchens = p.Kitchens,
                            Bathrooms = p.Bathrooms,
                            LivingRooms = p.LivingRooms,
                            Description = p.Description,
                            OwnerId = p.OwnerId,
                            ThemeImageUrl = p.Images.FirstOrDefault(img => img.IsTheme)!.ImageUrl,
                            PhoneNumber = _context.Users
                                .Where(u => u.Id == p.OwnerId)
                                .Select(u => u.PhoneNumber)
                                .FirstOrDefault(),
                            IsFavorite = currentUserId != null && userFavoriteIds.Contains(p.Id),
                            IsAvailable = p.IsAvailable
                        })
                        .ToList();

                    var response = new FiltersResponseDto<PrivatePropertyResponseDto>
                    {
                        PageNumber = filters.PageNumber,
                        PageSize = filters.PageSize,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        Properties = pagedProperties
                    };

                    return Ok(response);
                }
                else
                {
                    var pagedProperties = filteredQuery
                        .Skip(skip)
                        .Take(filters.PageSize)
                        .Select(p => new PropertyResponseDto
                        {
                            PropertyId = p.Id,
                            Title = p.Title,
                            City = p.City,
                            Village=p.Village,
                            Type=p.Type,
                            Price = p.Price,
                            Status = p.Status,
                            RentDuration = p.RentDuration,
                            Bedrooms = p.Bedrooms,
                            Kitchens = p.Kitchens,
                            Bathrooms = p.Bathrooms,
                            LivingRooms = p.LivingRooms,
                            Description = p.Description,
                            OwnerId = p.OwnerId,
                            ThemeImageUrl = p.Images.FirstOrDefault(img => img.IsTheme)!.ImageUrl,
                            PhoneNumber = _context.Users
                                .Where(u => u.Id == p.OwnerId)
                                .Select(u => u.PhoneNumber)
                                .FirstOrDefault(),
                            IsFavorite = currentUserId != null && userFavoriteIds.Contains(p.Id)
                        })
                        .ToList();

                    var response = new FiltersResponseDto<PropertyResponseDto>
                    {
                        PageNumber = filters.PageNumber,
                        PageSize = filters.PageSize,
                        TotalPages = totalPages,
                        TotalItems = totalItems,
                        Properties = pagedProperties
                    };

                    return Ok(response);
                }


            }
            catch (InvalidOperationException invOpEx)
            {
                return StatusCode(500, $"Database schema error: {invOpEx.Message}");
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, $"SQL Server error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
        [HttpGet("PropertyProfile/{id}")]
        public async Task<IActionResult> GetProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (property == null)
                return BadRequest(new { error = "Property not found or deleted." });

            // Get current user ID from token claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim.Value) : (int?)null;

            bool isFavorited = false;
            if (currentUserId.HasValue)
            {
                isFavorited = await _context.UserFavoriteProperties
                    .AnyAsync(f => f.UserId == currentUserId.Value && f.PropertyId == id);
            }

            var response = new PropertyProfileDto
            {
                Id = property.Id,
                ownerId= property.Owner.Id,
                Owner = property.Owner == null ? null : new OwnerDto
                {
                    Id = property.Owner.Id,
                    FirstName = property.Owner.FirstName,
                    LastName = property.Owner.LastName,
                    MobileNumber = property.Owner.MobileNumber
                },
                Title = property.Title,
                Description = property.Description,
                City = property.City,
                Village = property.Village?.Trim(),
                Type=property.Type,
                Street = property.Street,
                Price = property.Price,
                Status = property.Status,
                CreatedAt = property.CreatedAt,
                AvailabilityDate = property.AvailabilityDate,
                RentDuration = property.RentDuration,
                Bedrooms = property.Bedrooms,
                Kitchens = property.Kitchens,
                Bathrooms = property.Bathrooms,
                LivingRooms = property.LivingRooms,
                Longitude = property.Longitude ?? 0,
                Latitude = property.Latitude ?? 0,
                IsAvailable = property.IsAvailable,
                Images = property.Images?.Select(img => new ImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    IsTheme = img.IsTheme
                }).ToList(),
                IsFavoritedByCurrentUser = isFavorited
            };

            return Ok(response);
        }



        [HttpGet("top-cities")]
        public async Task<IActionResult> GetTopCities()
        {
            var topCities = await _context.Properties
                .GroupJoin(_context.Cities,
                    property => property.City,
                    city => city.City,
                    (property, cityGroup) => new { property, cityGroup })
                .SelectMany(
                    x => x.cityGroup.DefaultIfEmpty(),
                    (property, city) => new { property.property.City, CityId = city != null ? city.Id : (int?)null, ImageUrl = city != null ? city.ImageUrl : null })
                .GroupBy(x => new { x.City, x.CityId, x.ImageUrl })
                .Select(g => new
                {
                    Id = g.Key.CityId,
                    City = g.Key.City,
                    ImageUrl = g.Key.ImageUrl,
                    ListingCount = g.Count()
                })
                .OrderByDescending(g => g.ListingCount)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                Cities = topCities
            });
        }
    }
}
