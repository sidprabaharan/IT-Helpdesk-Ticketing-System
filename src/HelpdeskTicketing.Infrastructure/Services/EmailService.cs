using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string WebsiteUrl { get; set; }
        public string SupportTeamEmail { get; set; }
        public bool EnableSsl { get; set; } = true;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendTicketCreatedAsync(Ticket ticket)
        {
            var subject = $"New Ticket Created: #{ticket.Id} - {ticket.Title}";
            var body = new StringBuilder();
            body.AppendLine("<h2>New Ticket Created</h2>");
            body.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            body.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            body.AppendLine($"<p><strong>Priority:</strong> {ticket.Priority}</p>");
            body.AppendLine($"<p><strong>Category:</strong> {ticket.Category}</p>");
            body.AppendLine($"<p><strong>Status:</strong> {ticket.Status}</p>");
            body.AppendLine($"<p><strong>Created by:</strong> {ticket.Requester.FirstName} {ticket.Requester.LastName}</p>");
            body.AppendLine($"<p><strong>Description:</strong><br/>{ticket.Description}</p>");
            body.AppendLine($"<p>Please <a href='{_emailSettings.WebsiteUrl}/tickets/details/{ticket.Id}'>click here</a> to view the ticket details.</p>");

            // Send to requester
            await SendEmailAsync(ticket.Requester.Email, subject, body.ToString());
            
            // Send to support team
            await SendEmailAsync(_emailSettings.SupportTeamEmail, subject, body.ToString());
        }

        public async Task SendTicketAssignedAsync(Ticket ticket)
        {
            if (ticket.AssignedTo == null) return;

            var subject = $"Ticket Assigned: #{ticket.Id} - {ticket.Title}";
            var body = new StringBuilder();
            body.AppendLine("<h2>Ticket Assigned to You</h2>");
            body.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            body.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            body.AppendLine($"<p><strong>Priority:</strong> {ticket.Priority}</p>");
            body.AppendLine($"<p><strong>Category:</strong> {ticket.Category}</p>");
            body.AppendLine($"<p><strong>Status:</strong> {ticket.Status}</p>");
            body.AppendLine($"<p><strong>Created by:</strong> {ticket.Requester.FirstName} {ticket.Requester.LastName}</p>");
            body.AppendLine($"<p><strong>Description:</strong><br/>{ticket.Description}</p>");
            body.AppendLine($"<p>Please <a href='{_emailSettings.WebsiteUrl}/tickets/details/{ticket.Id}'>click here</a> to view the ticket details.</p>");

            // Send to assigned agent
            await SendEmailAsync(ticket.AssignedTo.Email, subject, body.ToString());
            
            // Send notification to requester
            var requesterSubject = $"Your Ticket #{ticket.Id} has been assigned";
            var requesterBody = new StringBuilder();
            requesterBody.AppendLine("<h2>Your Ticket Has Been Assigned</h2>");
            requesterBody.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            requesterBody.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            requesterBody.AppendLine($"<p><strong>Assigned to:</strong> {ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}</p>");
            requesterBody.AppendLine($"<p>Please <a href='{_emailSettings.WebsiteUrl}/tickets/details/{ticket.Id}'>click here</a> to track your ticket status.</p>");
            
            await SendEmailAsync(ticket.Requester.Email, requesterSubject, requesterBody.ToString());
        }

        public async Task SendTicketUpdatedAsync(Ticket ticket, string updatedProperty, string oldValue, string newValue)
        {
            var subject = $"Ticket Updated: #{ticket.Id} - {ticket.Title}";
            var body = new StringBuilder();
            body.AppendLine("<h2>Ticket Update Notification</h2>");
            body.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            body.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            body.AppendLine($"<p><strong>Updated Field:</strong> {updatedProperty}</p>");
            body.AppendLine($"<p><strong>Previous Value:</strong> {oldValue}</p>");
            body.AppendLine($"<p><strong>New Value:</strong> {newValue}</p>");
            body.AppendLine($"<p><strong>Current Status:</strong> {ticket.Status}</p>");
            body.AppendLine($"<p>Please <a href='{_emailSettings.WebsiteUrl}/tickets/details/{ticket.Id}'>click here</a> to view the ticket details.</p>");

            // Send to requester
            await SendEmailAsync(ticket.Requester.Email, subject, body.ToString());
            
            // Send to assigned agent if exists
            if (ticket.AssignedTo != null)
            {
                await SendEmailAsync(ticket.AssignedTo.Email, subject, body.ToString());
            }
        }

        public async Task SendTicketCommentAddedAsync(Ticket ticket, TicketComment comment)
        {
            // Skip sending notification for internal comments
            if (comment.IsInternal) return;
            
            var subject = $"New Comment on Ticket: #{ticket.Id} - {ticket.Title}";
            var body = new StringBuilder();
            body.AppendLine("<h2>New Comment Added</h2>");
            body.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            body.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            body.AppendLine($"<p><strong>Comment by:</strong> {comment.Author.FirstName} {comment.Author.LastName}</p>");
            body.AppendLine($"<p><strong>Comment:</strong><br/>{comment.Content}</p>");
           body.AppendLine($"<p>Please <a href='{_emailSettings.WebsiteUrl}/tickets/details/{ticket.Id}'>click here</a> to view the ticket and respond.</p>");

            // Send to requester (if comment is from support)
            if (comment.Author.Id != ticket.RequesterId)
            {
                await SendEmailAsync(ticket.Requester.Email, subject, body.ToString());
            }
            
            // Send to assigned agent (if comment is from requester)
            if (ticket.AssignedTo != null && comment.Author.Id == ticket.RequesterId)
            {
                await SendEmailAsync(ticket.AssignedTo.Email, subject, body.ToString());
            }
        }

        public async Task SendTicketResolvedAsync(Ticket ticket)
        {
            var subject = $"Ticket Resolved: #{ticket.Id} - {ticket.Title}";
            var body = new StringBuilder();
            body.AppendLine("<h2>Ticket Has Been Resolved</h2>");
            body.AppendLine($"<p><strong>Ticket ID:</strong> #{ticket.Id}</p>");
            body.AppendLine($"<p><strong>Title:</strong> {ticket.Title}</p>");
            body.AppendLine($"<p><strong>Resolved by:</strong> {ticket.AssignedTo?.FirstName} {ticket.AssignedTo?.LastName}</p>");
            body.AppendLine($"<p><strong>Resolution Time:</strong> {ticket.ActualResolutionTimeInMinutes} minutes</p>");
            body.AppendLine("<p>Please confirm if this issue has been resolved to your satisfaction. You can:</p>");
            body.AppendLine("<ul>");
            body.AppendLine($"<li><a href='{_emailSettings.WebsiteUrl}/tickets/confirm-resolution/{ticket.Id}'>Confirm Resolution</a> - This will close the ticket.</li>");
            body.AppendLine($"<li><a href='{_emailSettings.WebsiteUrl}/tickets/reopen/{ticket.Id}'>Reopen Ticket</a> - If the issue is not fully resolved.</li>");
            body.AppendLine("</ul>");
            
            // Send to requester
            await SendEmailAsync(ticket.Requester.Email, subject, body.ToString());
        }

        public async Task SendUserRegistrationAsync(ApplicationUser user, string callbackUrl)
        {
            var subject = "Welcome to IT Helpdesk - Confirm Your Email";
            var body = new StringBuilder();
            body.AppendLine("<h2>Welcome to IT Helpdesk Ticketing System</h2>");
            body.AppendLine($"<p>Hello {user.FirstName},</p>");
            body.AppendLine("<p>Thank you for registering with our IT Helpdesk system. To complete your registration, please confirm your email address.</p>");
            body.AppendLine($"<p><a href='{callbackUrl}'>Click here to confirm your email</a></p>");
            body.AppendLine("<p>If you did not register for this account, please ignore this email.</p>");
            
            await SendEmailAsync(user.Email, subject, body.ToString());
        }

        public async Task SendPasswordResetAsync(ApplicationUser user, string callbackUrl)
        {
            var subject = "IT Helpdesk - Password Reset Request";
            var body = new StringBuilder();
            body.AppendLine("<h2>Password Reset Request</h2>");
            body.AppendLine($"<p>Hello {user.FirstName},</p>");
            body.AppendLine("<p>We received a request to reset your password. Please click the link below to create a new password:</p>");
            body.AppendLine($"<p><a href='{callbackUrl}'>Reset your password</a></p>");
            body.AppendLine("<p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>");
            
            await SendEmailAsync(user.Email, subject, body.ToString());
        }

        public async Task SendLoginWarningAsync(ApplicationUser user, string ipAddress, string location)
        {
            var subject = "IT Helpdesk - Security Alert: Multiple Failed Login Attempts";
            var body = new StringBuilder();
            body.AppendLine("<h2>Security Alert: Multiple Failed Login Attempts</h2>");
            body.AppendLine($"<p>Hello {user.FirstName},</p>");
            body.AppendLine("<p>We've detected multiple failed login attempts to your IT Helpdesk account.</p>");
            body.AppendLine("<p><strong>Details:</strong></p>");
            body.AppendLine("<ul>");
            body.AppendLine($"<li>IP Address: {ipAddress}</li>");
            body.AppendLine($"<li>Location: {location}</li>");
            body.AppendLine($"<li>Time: {DateTime.UtcNow}</li>");
            body.AppendLine("</ul>");
            body.AppendLine("<p>If this was you, you can ignore this message. Your account has been temporarily locked for security. Please try again in 30 minutes.</p>");
            body.AppendLine("<p>If this wasn't you, we recommend changing your password immediately.</p>");
            body.AppendLine($"<p><a href='{_emailSettings.WebsiteUrl}/Account/ResetPassword'>Reset Your Password</a></p>");
            
            await SendEmailAsync(user.Email, subject, body.ToString());
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                
                message.To.Add(new MailAddress(to));

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                    client.EnableSsl = _emailSettings.EnableSsl;

                    await client.SendMailAsync(message);
                    _logger.LogInformation($"Email sent successfully to {to}, subject: {subject}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}, subject: {subject}");
                // Don't throw - email failure shouldn't break the application
            }
        }
    }
}
