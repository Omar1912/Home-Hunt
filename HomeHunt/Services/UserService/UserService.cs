using HomeHunt.Data;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HomeHunt.Services
{
    public class UserService : IUserService
    {
        private readonly HomeHuntDBContext _context;

        public UserService(HomeHuntDBContext context)
        {
            _context = context;
        }

        public async Task<UserEntity> GetUserByIdAsync(int userId)//by IsActive we mean id account for user is deleted or not 
        {
            return await _context.Users
                                 .Where(u => u.Id == userId && u.IsActive)
                                 .FirstOrDefaultAsync();
        }
    }
}
