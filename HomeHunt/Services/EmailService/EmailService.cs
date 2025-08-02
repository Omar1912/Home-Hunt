using HomeHunt.Models;
using HomeHunt.Models.Entities;
using HomeHunt.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HomeHunt.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(string sendGridApiKey, string fromEmail, string fromName)
        {
            _sendGridApiKey = sendGridApiKey ?? throw new ArgumentNullException(nameof(sendGridApiKey));
            _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
            _fromName = fromName ?? throw new ArgumentNullException(nameof(fromName));
        }
        // Send Singup Verification Code

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var subject = "Verify Your HomeHunt Email";
            var to = new EmailAddress(toEmail);

            var plainTextContent = $"Your verification code is: {code}";
            var htmlContent = $"<p>Your verification code is: <strong>{code}</strong></p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception("Failed to send email: {response.StatusCode}");
            }
        }

        // Send Password Reset Email
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Reset Your HomeHunt Password";
            var resetLink = $"http://localhost:5173/credintials/resetpassword?token={resetToken}";
            var htmlContent = $"<p>Click the link below to reset your password:</p><p><a href=\"{resetLink}\">Reset Password</a></p>";
            var plainTextContent = $"Click the link to reset your password: {resetLink}";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to send email: {response.StatusCode}");
            }
        }

        public async Task SendTourRequestNotificationAsync(
            string toEmail,
            string requesterUsername,
            string phoneNumber,
            string requesterEmail,
            TourRequestDTO requestDto)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = $"{requesterUsername} would like to tour your property";

            var datesList = new List<string>();

            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate1))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate1).ToString("dddd, MMMM dd, yyyy"));

            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate2))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate2).ToString("dddd, MMMM dd, yyyy"));

            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate3))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate3).ToString("dddd, MMMM dd, yyyy"));

            string preferredDatesText = datesList.Count > 0
                ? "They would like to schedule a tour on the following dates:\n- " + string.Join("\n- ", datesList)
                : "No preferred dates were provided.";

            string preferredDatesHtml = datesList.Count > 0
                ? "<ul><li>" + string.Join("</li><li>", datesList) + "</li></ul>"
                : "<em>No preferred dates were provided.</em>";

            // Notes
            string notesText = string.IsNullOrWhiteSpace(requestDto.Notes)
                ? "No additional notes were provided."
                : $"Notes: {requestDto.Notes}";

            string notesHtml = string.IsNullOrWhiteSpace(requestDto.Notes)
                ? "<em>No additional notes were provided.</em>"
                : $"<strong>Notes:</strong> {requestDto.Notes}";

            // Plain text content
            var plainTextContent =
                $"{requesterUsername} has requested to tour your property.\n\n" +
                $"Contact Details:\n" +
                $"- Phone: {phoneNumber}\n" +
                $"- Email: {requesterEmail}\n\n" +
                $"{preferredDatesText}\n\n" +
                $"{notesText}\n\n" +
                "Please do not reply to this email.";

            // HTML content
            var htmlContent =
                $"<p><strong>{requesterUsername}</strong> has requested to tour your property.</p>" +
                $"<p><strong>Contact Details:</strong><br>" +
                $"Phone: {phoneNumber}<br>" +
                $"Email: {requesterEmail}</p>" +
                $"<p><strong>Preferred Dates:</strong><br>{preferredDatesHtml}</p>" +
                $"<p>{notesHtml}</p>" +
                $"<p><em>Please do not reply to this email.</em></p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Console.WriteLine($"Warning: Failed to send email. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when sending email: {ex.Message}");
            }
        }
        public async Task SendTourRequestConfirmationAsync(
           string toEmail,
           TourRequestDTO requestDto,
           UserEntity owner,
           PropertyEntity property)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Your Tour Request Confirmation";

            var datesList = new List<string>();

            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate1))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate1).ToString("dddd, MMMM dd, yyyy"));
            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate2))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate2).ToString("dddd, MMMM dd, yyyy"));
            if (!string.IsNullOrWhiteSpace(requestDto.PreferredDate3))
                datesList.Add(DateTime.Parse(requestDto.PreferredDate3).ToString("dddd, MMMM dd, yyyy"));

            string preferredDatesText = datesList.Count > 0
                ? "You have requested a tour on the following dates:\n- " + string.Join("\n- ", datesList)
                : "No preferred dates were provided.";

            string preferredDatesHtml = datesList.Count > 0
                ? "<ul><li>" + string.Join("</li><li>", datesList) + "</li></ul>"
                : "<em>No preferred dates were provided.</em>";

            string notesText = string.IsNullOrWhiteSpace(requestDto.Notes)
                ? "No additional notes were provided."
                : $"Notes: {requestDto.Notes}";

            string notesHtml = string.IsNullOrWhiteSpace(requestDto.Notes)
                ? "<em>No additional notes were provided.</em>"
                : $"<strong>Notes:</strong> {requestDto.Notes}";

            string propertyDetailsText = $"Property details:\n" +
                                         $"- Title: {property.Title ?? "N/A"}\n" +
                                         $"- City: {property.City ?? "N/A"}\n" +
                                         $"- Village: {property.Village ?? "N/A"}\n" +
                                         $"- Description: {(string.IsNullOrWhiteSpace(property.Description) ? "N/A" : property.Description)}";

            string propertyDetailsHtml = $"<p><strong>Property details:</strong></p>" +
                                         $"<ul>" +
                                         $"<li>Title: {property.Title ?? "N/A"}</li>" +
                                         $"<li>City: {property.City ?? "N/A"}</li>" +
                                         $"<li>Village: {property.Village ?? "N/A"}</li>" +
                                         $"<li>Description: {(string.IsNullOrWhiteSpace(property.Description) ? "N/A" : property.Description)}</li>" +
                                         $"</ul>";

            string ownerContactText = $"Owner contact details:\n" +
                                      $"- Name: {owner.FirstName + " " + owner.LastName}\n" +
                                      $"- Email: {owner.Email}\n" +
                                      $"- Phone: {owner.PhoneNumber ?? "N/A"}";

            string ownerContactHtml = $"<p><strong>Owner contact details:</strong></p>" +
                                      $"<ul>" +
                                      $"<li>Name: {owner.FirstName + " " + owner.LastName}</li>" +
                                      $"<li>Email: {owner.Email}</li>" +
                                      $"<li>Phone: {owner.PhoneNumber ?? "N/A"}</li>" +
                                      $"</ul>";

            var plainTextContent =
                $"You have requested to tour a property with the following details:\n\n" +
                $"{propertyDetailsText}\n\n" +
                $"{preferredDatesText}\n\n" +
                $"{notesText}\n\n" +
                $"{ownerContactText}\n\n" +
                "Thank you for using HomeHunt!";

            var htmlContent =
                $"<p>You have requested to tour a property with the following details:</p>" +
                $"{propertyDetailsHtml}" +
                $"<p><strong>Preferred Dates:</strong>{preferredDatesHtml}</p>" +
                $"<p>{notesHtml}</p>" +
                $"{ownerContactHtml}" +
                $"<p>Thank you for using <strong>HomeHunt</strong>!</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            try
            {
                var response = await client.SendEmailAsync(msg);
                if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Console.WriteLine($"Warning: Failed to send email confirmation. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when sending confirmation email: {ex.Message}");
            }
        }


        // Notify owner that a report was submitted on their property
        public async Task SendReportNotificationAsync(string toEmail, string propertyTitle, string ownerName)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "A Report Has Been Submitted on Your Property";

            var plainTextContent =
                $"Hello {ownerName},\n\n" +
                $"We want to inform you that your property titled '{propertyTitle}' has been reported by a user.\n" +
                $"Please review your listing to ensure it complies with our platform guidelines.\n" +
                $"If you believe this report was made in error, feel free to contact us to review the case.\n\n" +
                $"Thank you,\nHomeHunt Support Team";

            var htmlContent =
                $"<p>Hello {ownerName},</p>" +
                $"<p>We want to inform you that your property titled <strong>{propertyTitle}</strong> has been reported by a user.</p>" +
                $"<p>Please review your listing to ensure it complies with our platform guidelines.</p>" +
                $"<p>If you believe this report was made in error, feel free to contact us to review the case.</p>" +
                $"<p>Thank you,<br>HomeHunt Support Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }

        // Warning after property reaches report threshold
        public async Task SendWarningEmailAsync(string toEmail, string propertyTitle, string ownerName)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Warning: Your Property Has Multiple Reports";

            var plainTextContent =
                $"Hello {ownerName},\n\n" +
                $"We noticed that your property titled '{propertyTitle}' has received multiple reports from users.\n" +
                $"Please review your listing as soon as possible to ensure it complies with our platform guidelines.\n" +
                $"If you believe these reports are incorrect, feel free to reach out to our support team.\n\n" +
                $"Thank you,\nHomeHunt Support Team";

            var htmlContent =
                $"<p>Hello {ownerName},</p>" +
                $"<p>We noticed that your property titled <strong>{propertyTitle}</strong> has received multiple reports from users.</p>" +
                $"<p>Please review your listing as soon as possible to ensure it complies with our platform guidelines.</p>" +
                $"<p>If you believe these reports are incorrect, feel free to reach out to our support team.</p>" +
                $"<p>Thank you,<br>HomeHunt Support Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }

        // Notify owner that their property has been deleted
        public async Task SendPropertyDeletedEmailAsync(string toEmail, string propertyTitle, string ownerName)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Your Property Has Been Removed";

            var plainTextContent =
                $"Hello {ownerName},\n\n" +
                $"We want to inform you that your property titled '{propertyTitle}' has been removed from the platform due to exceeding the allowed number of reports.\n" +
                $"This action was taken based on our platform's safety and trust policy.\n" +
                $"If you believe this was a mistake, please contact us to review the case.\n\n" +
                $"Thank you,\nHomeHunt Support Team";

            var htmlContent =
                $"<p>Hello {ownerName},</p>" +
                $"<p>We want to inform you that your property titled <strong>{propertyTitle}</strong> has been removed from the platform due to exceeding the allowed number of reports.</p>" +
                $"<p>This action was taken based on our platform's safety and trust policy.</p>" +
                $"<p>If you believe this was a mistake, please contact us to review the case.</p>" +
                $"<p>Thank you,<br>HomeHunt Support Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }

        // Notify user that their account was deleted
        public async Task SendAccountDeletedEmailAsync(string toEmail, string ownerName)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Your Account Has Been Deleted";

            var plainTextContent =
                $"Hello {ownerName},\n\n" +
                $"We regret to inform you that your account has been deleted due to repeated violations based on property reports.\n" +
                $"All active listings associated with your account have also been removed.\n" +
                $"If you believe this action was taken in error, please contact us to review your case.\n\n" +
                $"Thank you,\nHomeHunt Support Team";

            var htmlContent =
                $"<p>Hello {ownerName},</p>" +
                $"<p>We regret to inform you that your account has been deleted due to repeated violations based on property reports.</p>" +
                $"<p>All active listings associated with your account have also been removed.</p>" +
                $"<p>If you believe this action was taken in error, please contact us to review your case.</p>" +
                $"<p>Thank you,<br>HomeHunt Support Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }

        // Notify the user that their account received a strike after one of their properties was deleted
        public async Task SendAccountReportNotificationAsync(string toEmail, string ownerName)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var subject = "Your Account Has Received a Strike";

            var plainTextContent =
                $"Hello {ownerName},\n\n" +
                $"We would like to inform you that your account has received a strike after one of your properties was removed due to multiple reports.\n" +
                $"Please ensure that your future listings comply with our platform’s rules and community standards.\n\n" +
                $"Thank you,\nHomeHunt Support Team";

            var htmlContent =
                $"<p>Hello {ownerName},</p>" +
                $"<p>We would like to inform you that your account has received a <strong>strike</strong> after one of your properties was removed due to multiple reports.</p>" +
                $"<p>Please ensure that your future listings comply with our platform’s rules and community standards.</p>" +
                $"<p>Thank you,<br>HomeHunt Support Team</p>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }
    }
}