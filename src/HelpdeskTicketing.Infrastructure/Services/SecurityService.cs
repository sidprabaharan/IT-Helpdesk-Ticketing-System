using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Data;
using HelpdeskTicketing.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Infrastructure.Services
{
    public class SecuritySettings
    {
        public int MaxFailedLoginAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 30;
        public bool EnableIpLocationCheck { get; set; } = true;
        public string GeoIpApiUrl { get; set; }
        public string GeoIpApiKey { get; set; }
    }

    public class SecurityService : ISecurityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<SecurityService> _logger;
        private readonly SecuritySettings _securitySettings;
        private readonly HttpClient _httpClient;

        public SecurityService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<SecurityService> logger,
            IOptions<SecuritySettings> securitySettings,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _securitySettings = securitySettings.Value;
            _httpClient = httpClientFactory.CreateClient("GeoIpApi");
        }

        public async Task<bool> ValidateLoginAttemptAsync(ApplicationUser user, string password)
        {
            // Check if account is locked
            if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            {
                _logger.LogWarning($"Account {user.UserName} is locked until {user.LockoutUntil}");
                return false;
            }

            // Check password
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<bool> IsAccountLockedAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return false;

            return user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow;
        }

        public async Task<(ApplicationUser User, SignInResult Result)> AuthenticateAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return (null, SignInResult.Failed);
            }

            var result = await _signInManager.PasswordSignInAsync(username, password, false, true);
            
            if (result.Succeeded)
            {
                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                // Reset failed login attempts
                await ResetFailedLoginAttemptsAsync(user.Id);
            }

            return (user, result);
        }

        public async Task LogFailedLoginAttemptAsync(string username, string ipAddress)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return;

            // Increment failed login attempts
            user.FailedLoginAttempts++;
            
            // Lock account if max attempts reached
            if (user.FailedLoginAttempts >= _securitySettings.MaxFailedLoginAttempts)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(_securitySettings.LockoutDurationMinutes);
                _logger.LogWarning($"Account {username} locked due to {user.FailedLoginAttempts} failed login attempts");
                
                // Send security alert email
                var location = await GetUserLocationByIpAsync(ipAddress);
                await _emailService.SendLoginWarningAsync(user, ipAddress, location);
            }

            await _userManager.UpdateAsync(user);
        }

        public async Task ResetFailedLoginAttemptsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _userManager.UpdateAsync(user);
        }

        public async Task<string> GetUserLocationByIpAsync(string ipAddress)
        {
            if (!_securitySettings.EnableIpLocationCheck || string.IsNullOrEmpty(_securitySettings.GeoIpApiUrl))
            {
                return "Unknown";
            }

            try
            {
                var requestUrl = $"{_securitySettings.GeoIpApiUrl}/{ipAddress}";
                if (!string.IsNullOrEmpty(_securitySettings.GeoIpApiKey))
                {
                    requestUrl += $"?apiKey={_securitySettings.GeoIpApiKey}";
                }

                var response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var geoData = JsonSerializer.Deserialize<GeoIpData>(content);
                    
                    if (geoData != null)
                    {
                        return $"{geoData.City}, {geoData.Region}, {geoData.Country}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location data for IP: {IpAddress}", ipAddress);
            }

            return "Unknown";
        }

        public async Task<bool> IsIpSuspiciousAsync(string ipAddress)
        {
            // Here you could implement more advanced IP checking logic
            // such as checking against known malicious IPs, rate limiting, etc.
            
            // For now, we'll check how many failed login attempts came from this IP in the last hour
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            
            var loginAttempts = await _context.Set<LoginAttempt>()
                .CountAsync(a => a.IpAddress == ipAddress && 
                             a.AttemptTime > oneHourAgo && 
                             !a.Succeeded);
                             
            return loginAttempts > 10; // Arbitrary threshold
        }

        private class GeoIpData
        {
            public string Ip { get; set; }
            public string City { get; set; }
            public string Region { get; set; }
            public string Country { get; set; }
        }
    }

    // Additional model for tracking login attempts
    public class LoginAttempt
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public DateTime AttemptTime { get; set; }
        public bool Succeeded { get; set; }
        public string UserAgent { get; set; }
    }
}
