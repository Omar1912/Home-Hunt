using HomeHunt.Models.DTOs;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using HomeHunt.Services.Filters;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace HomeHuntTests_Filters
{
    public class FilterServiceTests
    {
        private readonly PropertyFilterService _filterService;

        public FilterServiceTests()
        {
            _filterService = new PropertyFilterService();
        }

        private List<PropertyEntity> GetTestProperties()
        {
            return new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    Id = 1,
                    City = "Ramallah",
                    Price = 1000,
                    Bedrooms = 2,
                    Status = "Available",
                    RentDuration = "Monthly",
                    Type = "Apartment",
                    IsDeleted = false,
                    IsAvailable = true,
                    Title = "Cozy Apartment"
                },
                new PropertyEntity
                {
                    Id = 2,
                    City = "Nablus",
                    Price = 500,
                    Bedrooms = 1,
                    Status = "Rented",
                    RentDuration = "Yearly",
                    Type = "House",
                    IsDeleted = false,
                    IsAvailable = false,
                    Title = "Small House"
                },
                new PropertyEntity
                {
                    Id = 3,
                    City = "Hebron",
                    Price = 800,
                    Bedrooms = 3,
                    Status = "Available",
                    RentDuration = "Monthly",
                    Type = "Villa",
                    IsDeleted = true, // deleted property
                    IsAvailable = true,
                    Title = "Luxury Villa"
                }
            };
        }

        [Fact]
        public void ApplyFilters_CityFilter_ReturnsCorrectResults()
        {
            // Arrange
            var filters = new FiltersDto { City = "Ramallah" };
            var properties = GetTestProperties().AsQueryable();

            // Act
            var result = _filterService.ApplyFilters(properties, filters).ToList();

            // Assert
            Assert.All(result, p => Assert.Contains("Ramallah", p.City));
            Assert.Equal(1, result.Count); // only ID 1, not ID 3 (deleted)
        }

        [Fact]
        public void ApplyFilters_MinMaxPriceFilter_ReturnsCorrectResults()
        {
            var filters = new FiltersDto { MinPrice = 900, MaxPrice = 1300 };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.InRange(p.Price, 900, 1300));
            Assert.DoesNotContain(result, p => p.Price < 900 || p.Price > 1300);
        }

        [Fact]
        public void ApplyFilters_BedroomsFilter_ReturnsCorrectResults()
        {
            var filters = new FiltersDto { Bedrooms = 2 };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.Equal(2, p.Bedrooms));
        }

        [Fact]
        public void ApplyFilters_HomeTypeFilter_ReturnsCorrectResults()
        {
            var filters = new FiltersDto { HomeType = "Apartment" };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.Contains("Apartment", p.Type));
        }

        [Fact]
        public void ApplyFilters_PrivateFilters_IsAvailable_ReturnsOnlyAvailable()
        {
            var filters = new PrivateFiltersDTO { IsAvailable = true };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.True(p.IsAvailable));
        }

        [Fact]
        public void ApplyFilters_DeletedProperties_AreExcluded()
        {
            var filters = new FiltersDto(); // no filters = all except deleted
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.DoesNotContain(result, p => p.IsDeleted);
        }

        [Fact]
        public void ApplyFilters_MultipleFiltersCombined_ReturnsCorrectlyFiltered()
        {
            var filters = new FiltersDto
            {
                City = "Ramallah",
                MinPrice = 100,
                MaxPrice = 1000,
                Bedrooms = 2,
                Status = "Available",
                RentDuration = "Monthly",
                HomeType = "Apartment"
            };

         
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }
        [Fact]
        public void ApplyFilters_EmptyFilters_ReturnsAllNonDeleted()
        {
            var filters = new FiltersDto();
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.Equal(2, result.Count); // Only 2 non-deleted properties
            Assert.All(result, p => Assert.False(p.IsDeleted));
        }

        [Fact]
        public void ApplyFilters_NoMatch_ReturnsEmpty()
        {
            var filters = new FiltersDto
            {
                City = "NonexistentCity",
                MinPrice = 9999,
                Bedrooms = 10,
                HomeType = "Castle"
            };

            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void ApplyFilters_NullFilters_ReturnsAllNonDeleted()
        {
            FiltersDto filters = null;
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters ?? new FiltersDto()).ToList();

            Assert.Equal(2, result.Count); // excludes deleted
        }

        [Fact]
        public void ApplyFilters_CitySubstringMatch_ReturnsCorrectResults()
        {
            var filters = new FiltersDto { City = "Ram" };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.Single(result);
            Assert.Contains("Ram", result[0].City);
        }

        [Fact]
        public void ApplyFilters_RentDurationFilter_ReturnsMonthlyOnly()
        {
            var filters = new FiltersDto { RentDuration = "Monthly" };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.Contains("Monthly", p.RentDuration));
        }

        [Fact]
        public void ApplyFilters_StatusFilter_ReturnsMatchingStatus()
        {
            var filters = new FiltersDto { Status = "Avail" };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.All(result, p => Assert.Contains("Avail", p.Status));
        }

        [Fact]
        public void ApplyFilters_PrivateFilters_IsAvailableFalse_ReturnsUnavailableOnly()
        {
            var filters = new PrivateFiltersDTO { IsAvailable = false };
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            Assert.Single(result);
            Assert.False(result[0].IsAvailable);
        }

        // Optional: If case sensitivity is relevant to your DB setup
        [Fact]
        public void ApplyFilters_CaseInsensitiveCityFilter_ReturnsNothingIfCaseDiffers()
        {
            var filters = new FiltersDto { City = "ramallah" }; // lower case
            var properties = GetTestProperties().AsQueryable();

            var result = _filterService.ApplyFilters(properties, filters).ToList();

            // Adjust based on actual DB collation behavior
            Assert.Empty(result);
        }
    }
}

