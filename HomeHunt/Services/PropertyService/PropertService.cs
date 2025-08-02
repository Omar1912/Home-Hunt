using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HomeHunt.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly HomeHuntDBContext _context;

        public PropertyService(HomeHuntDBContext context)
        {
            _context = context;
        }

        // Check if property exists
        public async Task<bool> PropertyExistsAsync(int propertyId)
        {
            return await _context.Properties
                                 .AnyAsync(p => p.Id == propertyId && !p.IsDeleted); // Check if property exists and is active(notdeleted)
        }

        // Get property owner ID
        public async Task<int> GetOwnerIdByPropertyIdAsync(int propertyId)
        {
            return await _context.Properties
                                 .Where(p => p.Id == propertyId)
                                 .Select(p => p.OwnerId)
                                 .FirstOrDefaultAsync();
        }
        public async Task<PropertyEntity?> GetPropertyByIdAsync(int propertyId)
        {
            return await _context.Properties
                                 .Where(p => p.Id == propertyId && !p.IsDeleted)
                                 .FirstOrDefaultAsync();
        }

    }
}
