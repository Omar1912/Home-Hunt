using HomeHunt.Models.Entities;
using System.Threading.Tasks;

namespace HomeHunt.Services.Interfaces
{
    public interface IPropertyService
    {
        Task<bool> PropertyExistsAsync(int propertyId);
        Task<int> GetOwnerIdByPropertyIdAsync(int propertyId);
        Task<PropertyEntity?> GetPropertyByIdAsync(int propertyId);


    }
}
