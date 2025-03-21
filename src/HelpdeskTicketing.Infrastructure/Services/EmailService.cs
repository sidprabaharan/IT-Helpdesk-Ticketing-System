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
            body.AppendLine($"<p>Please <a href='{_emailSettings.
