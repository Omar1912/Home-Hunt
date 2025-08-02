using HomeHunt.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeHunt.Data
{
    public interface IHomeHuntDBContext
    {
        DbSet<PropertyEntity> Properties { get; set; }
        DbSet<UserFavoriteProperties> UserFavoriteProperties { get; set; }
        DbSet<UserEntity> Users { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}