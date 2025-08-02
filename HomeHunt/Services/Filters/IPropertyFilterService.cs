using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;

namespace HomeHunt.Services.Filters
{
    public interface IPropertyFilterService
    {
        IQueryable<PropertyEntity> ApplyFilters(IQueryable<PropertyEntity> query, FiltersDto filters);
    }
}