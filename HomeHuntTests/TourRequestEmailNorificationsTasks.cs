using HomeHunt.Models;
using HomeHunt.Models.Entities;
using HomeHunt.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace HomeHuntTests_TourRequest
{
    public class TourRequestEmailNotificationsTasks
    {
        private readonly EmailService _emailService;

        public TourRequestEmailNotificationsTasks()
             
        {
            string apiKey = "";
            string fromEmail = "falasteenabuali@gmail.com";
            string fromName = "HomeHunt Support";

            _emailService = new EmailService(apiKey, fromEmail, fromName);
        }

        [Fact]
        public async Task SendTourRequestNotificationAsync_RealEmail_Success()
        {
            var dto = new TourRequestDTO
            {
                PreferredDate1 = "2025-06-20",
                PreferredDate2 = "2025-06-21",
                PreferredDate3 = null,
                Notes = "I would like to see the rooftop if possible."
            };

            string toEmail = "1210661@student.birzeit.edu";  
            string requesterUsername = "Falasteen Abu Ali";
            string phoneNumber = "+1234567890";
            string requesterEmail = "falasteenabuali@gmail.com";

            await _emailService.SendTourRequestNotificationAsync(
                toEmail, requesterUsername, phoneNumber, requesterEmail, dto);
        }

        [Fact]
        public async Task SendTourRequestConfirmationAsync_RealEmail_Success()
        {
            var dto = new TourRequestDTO
            {
                PreferredDate1 = "2025-06-20",
                PreferredDate2 = null,
                PreferredDate3 = null,
                Notes = "Can I bring a friend with me?"
            };

            var owner = new UserEntity
            {
                FirstName = "Owner",
                LastName = "Smith",
                Email = "1210661@student.birzeit.edu",
                PhoneNumber = "+1987654321"
            };

            var property = new PropertyEntity
            {
                Title = "Modern Studio in Ramallah",
                City = "Ramallah",
                Village = "Al-Tira",
                Description = "A modern, fully furnished studio apartment with balcony views."
            };

            string toEmail = "falasteenabuali@gmail.com"; 

            await _emailService.SendTourRequestConfirmationAsync(toEmail, dto, owner, property);
        }
    }
}


