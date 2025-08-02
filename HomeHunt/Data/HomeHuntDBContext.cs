using HomeHunt.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HomeHunt.Data
{
    public class HomeHuntDBContext : DbContext, IHomeHuntDBContext
    {
        public virtual DbSet<PropertyEntity> Properties { get; set; }
        public virtual DbSet<UserEntity> Users { get; set; }
        public virtual DbSet<UserFavoriteProperties> UserFavoriteProperties { get; set; }
        public virtual DbSet<TourEntity> TourRequests { get; set; }
        public virtual DbSet<CityEntity> Cities { get; set; }
        public virtual DbSet<PropertyImageEntity> PropertyImages { get; set; }
        public DbSet<VillageEntity> Villages { get; set; }
        public virtual DbSet<ReportEntity> Reports { get; set; }
 
        public HomeHuntDBContext(DbContextOptions<HomeHuntDBContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserFavoriteProperties>()
                .HasKey(uf => new { uf.UserId, uf.PropertyId });

            modelBuilder.Entity<UserFavoriteProperties>()
                    .HasOne(uf => uf.User)
                    .WithMany(u => u.FavoriteProperties)
                    .HasForeignKey(uf => uf.UserId)
                    .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<UserFavoriteProperties>()
                .HasOne(uf => uf.Property)
                .WithMany(p => p.FavoritedBy)
                .HasForeignKey(uf => uf.PropertyId)
                .OnDelete(DeleteBehavior.NoAction); 

            modelBuilder.Entity<TourEntity>()
                .Property(t => t.Status)
                .HasConversion<string>();

        }
    }
}

