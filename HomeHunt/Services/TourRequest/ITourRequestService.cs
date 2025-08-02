using HomeHunt.Models;
using System.Threading.Tasks;

namespace HomeHunt.Services.Interfaces
{
    public interface ITourRequestService
    {
        Task<String> CreateTourRequestAsync(int userId, TourRequestDTO requestDto);
    }
}
