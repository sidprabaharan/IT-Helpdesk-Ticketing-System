using HelpdeskTicketing.Core.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Services.Interfaces
{
    public interface ISecurityService
    {
        Task<bool> ValidateLoginAttemptAsync(ApplicationUser user, string password);
        Task<bool> IsAccountLockedAsync(string username);
        Task<(ApplicationUser User, SignInResult Result)> AuthenticateAsync(string username, string password);
        Task LogFailedLoginAttemptAsync(string username, string ipAddress);
        Task ResetFailedLoginAttemptsAsync(string userId);
        Task<string> GetUserLocationByIpAsync(string ipAddress);
        Task<bool> IsIpSuspiciousAsync(string ipAddress);
    }
}
