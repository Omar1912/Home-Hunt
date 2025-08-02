using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Filters;

namespace HomeHunt.Services
{
    public class PropertyFilterService : IPropertyFilterService
    {
        public IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, FiltersDto filters)
        {
            query = query.Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(filters.City))
            {
                query = query.Where(p => p.City.Contains(filters.City));
            }
            if (!string.IsNullOrEmpty(filters.village))
            {
                query = query.Where(p => p.Village.Contains(filters.village));
            }

            if (filters.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filters.MinPrice);
            }

            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filters.MaxPrice);
            }

            if (filters.Bedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms == filters.Bedrooms);
            }

            if (!string.IsNullOrEmpty(filters.Status))
            {
                query = query.Where(p => p.Status.Contains(filters.Status));
            }

            if (!string.IsNullOrEmpty(filters.RentDuration))
            {
                query = query.Where(p => p.RentDuration.Contains(filters.RentDuration));
            }

            if (!string.IsNullOrEmpty(filters.HomeType))
            {
                query = query.Where(p => p.Type.Contains(filters.HomeType));
            }
            if (filters.kitchens.HasValue)
            {
                query = query.Where(p => p.Kitchens == filters.kitchens );
            }

            if (filters.Bathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms == filters.Bathrooms.Value);
            }

            if (filters.LivingRooms.HasValue)
            {
                query = query.Where(p => p.LivingRooms == filters.LivingRooms.Value);
            }



            // Apply IsAvailable only if filters is of type PrivateFiltersDto
            if (filters is PrivateFiltersDTO privateFilters && privateFilters.IsAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == privateFilters.IsAvailable.Value);
            }
            return query;
        }
    }
}

