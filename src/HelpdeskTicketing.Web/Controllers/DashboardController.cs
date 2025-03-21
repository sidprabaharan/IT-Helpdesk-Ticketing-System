using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HelpdeskTicketing.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ITicketRepository ticketRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _ticketRepository = ticketRepository;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);
                var isSupport = User.IsInRole("Support") || User.IsInRole("Admin");

                var dashboardViewModel = new DashboardViewModel
                {
                    UserName = $"{user.FirstName} {user.LastName}",
                    IsSupport = isSupport
                };

                if (isSupport)
                {
                    // Support staff sees overall ticket metrics
                    dashboardViewModel.NewTickets = await _ticketRepository.CountAsync(t => t.Status == TicketStatus.New);
                    dashboardViewModel.AssignedTickets = await _ticketRepository.CountAsync(t => t.Status == TicketStatus.Assigned);
                    dashboardViewModel.InProgressTickets = await _ticketRepository.CountAsync(t => t.Status == TicketStatus.InProgress);
                    dashboardViewModel.ResolvedTickets = await _ticketRepository.CountAsync(t => t.Status == TicketStatus.Resolved);
                    dashboardViewModel.TotalTickets = await _ticketRepository.CountAsync();
                    
                    // Calculate resolution metrics
                    var resolvedTickets = await _ticketRepository.FindAsync(t => 
                        t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed);
                    
                    if (resolvedTickets.Any())
                    {
                        dashboardViewModel.AverageResolutionTime = (int)resolvedTickets
                            .Where(t => t.ActualResolutionTimeInMinutes.HasValue)
                            .Average(t => t.ActualResolutionTimeInMinutes.Value);
                            
                        dashboardViewModel.TicketsResolvedToday = resolvedTickets
                            .Count(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == DateTime.Today);
                    }

                    // Get assigned tickets for the support staff
                    var assignedToUser = await _ticketRepository.GetAssignedToUserAsync(userId);
                    dashboardViewModel.MyAssignedTickets = assignedToUser
                        .Select(t => new TicketViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Status = t.Status.ToString(),
                            Priority = t.Priority.ToString(),
                            Category = t.Category.ToString(),
                            CreatedAt = t.CreatedAt,
                            LastUpdatedAt = t.LastUpdatedAt,
                            RequesterName = $"{t.Requester.FirstName} {t.Requester.LastName}"
                        }).ToList();

                    // Get recent tickets for all support staff
                    var recentTickets = await _ticketRepository.GetPagedAsync(1, 10, null, "CreatedAt", false);
                    dashboardViewModel.RecentTickets = recentTickets
                        .Select(t => new TicketViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Status = t.Status.ToString(),
                            Priority = t.Priority.ToString(),
                            Category = t.Category.ToString(),
                            CreatedAt = t.CreatedAt,
                            LastUpdatedAt = t.LastUpdatedAt,
                            RequesterName = $"{t.Requester.FirstName} {t.Requester.LastName}",
                            AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null
                        }).ToList();
                }
                else
                {
                    // Regular users see only their own tickets
                    var userTickets = await _ticketRepository.GetRequestedByUserAsync(userId);
                    
                    dashboardViewModel.NewTickets = userTickets.Count(t => t.Status == TicketStatus.New);
                    dashboardViewModel.InProgressTickets = userTickets.Count(t => 
                        t.Status == TicketStatus.Assigned || t.Status == TicketStatus.InProgress);
                    dashboardViewModel.ResolvedTickets = userTickets.Count(t => t.Status == TicketStatus.Resolved);
                    dashboardViewModel.TotalTickets = userTickets.Count();
                    
                    // Get recent tickets for the user
                    dashboardViewModel.MyTickets = userTickets
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(10)
                        .Select(t => new TicketViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Status = t.Status.ToString(),
                            Priority = t.Priority.ToString(),
                            Category = t.Category.ToString(),
                            CreatedAt = t.CreatedAt,
                            LastUpdatedAt = t.LastUpdatedAt,
                            AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null
                        }).ToList();
                }

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View("Error");
            }
        }
    }

    public class DashboardViewModel
    {
        public string UserName { get; set; }
        public bool IsSupport { get; set; }
        
        // Ticket counts
        public int NewTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int TotalTickets { get; set; }
        
        // Performance metrics
        public int AverageResolutionTime { get; set; }
        public int TicketsResolvedToday { get; set; }
        
        // User-specific tickets
        public List<TicketViewModel> MyTickets { get; set; } = new List<TicketViewModel>();
        public List<TicketViewModel> MyAssignedTickets { get; set; } = new List<TicketViewModel>();
        public List<TicketViewModel> RecentTickets { get; set; } = new List<TicketViewModel>();
    }

    public class TicketViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string RequesterName { get; set; }
        public string AssignedToName { get; set; }
    }
}
