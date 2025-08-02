using HomeHunt.Models.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace HomeHunt.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetToken);
        Task SendVerificationCodeAsync(string toEmail, string code);
        Task SendTourRequestNotificationAsync(string ownerEmail, string requesterName, string requesterPhone, string requesterEmail, TourRequestDTO tourRequest);
        Task SendTourRequestConfirmationAsync(string requesterEmail, TourRequestDTO requestDto, UserEntity owner, PropertyEntity property);
        Task SendReportNotificationAsync(string toEmail, string propertyTitle, string ownerName);
        Task SendWarningEmailAsync(string toEmail, string propertyTitle, string ownerName);
        Task SendPropertyDeletedEmailAsync(string toEmail, string propertyTitle, string ownerName);
        Task SendAccountDeletedEmailAsync(string toEmail, string ownerName);
        Task SendAccountReportNotificationAsync(string toEmail, string ownerName);
    }
}