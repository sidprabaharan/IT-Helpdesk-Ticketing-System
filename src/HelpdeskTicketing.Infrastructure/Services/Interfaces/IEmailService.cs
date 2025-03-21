using HelpdeskTicketing.Core.Models;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendTicketCreatedAsync(Ticket ticket);
        Task SendTicketAssignedAsync(Ticket ticket);
        Task SendTicketUpdatedAsync(Ticket ticket, string updatedProperty, string oldValue, string newValue);
        Task SendTicketCommentAddedAsync(Ticket ticket, TicketComment comment);
        Task SendTicketResolvedAsync(Ticket ticket);
        Task SendUserRegistrationAsync(ApplicationUser user, string callbackUrl);
        Task SendPasswordResetAsync(ApplicationUser user, string callbackUrl);
        Task SendLoginWarningAsync(ApplicationUser user, string ipAddress, string location);
    }
}
