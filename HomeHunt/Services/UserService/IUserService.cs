using HomeHunt.Models.Entities;
using System.Threading.Tasks;

namespace HomeHunt.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserEntity?> GetUserByIdAsync(int userId);
    }
}

