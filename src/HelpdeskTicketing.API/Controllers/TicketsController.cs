using HelpdeskTicketing.Core.Models;
using HelpdeskTicketing.Infrastructure.Repositories.Interfaces;
using HelpdeskTicketing.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HelpdeskTicketing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(
            ITicketRepository ticketRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TicketsController> logger)
        {
            _ticketRepository = ticketRepository;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: api/tickets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = null,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] bool ascending = false)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var isSupport = User.IsInRole("Support") || User.IsInRole("Admin");

                // Create filter based on user role and status parameter
                System.Linq.Expressions.Expression<Func<Ticket, bool>> filter = null;

                if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, true, out var ticketStatus))
                {
                    filter = t => t.Status == ticketStatus;
                }

                // Regular users can only see their own tickets
                if (!isSupport)
                {
                    var userFilter = filter;
                    filter = t => t.RequesterId == userId;
                    
                    if (userFilter != null)
                    {
                        // Combine filters
                        filter = t => t.RequesterId == userId && userFilter.Compile()(t);
                    }
                }

                var tickets = await _ticketRepository.GetPagedAsync(page, pageSize, filter, sortBy, ascending);
                var totalCount = await _ticketRepository.CountAsync(filter);

                var result = tickets.Select(t => new TicketDto
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

                // Add pagination headers
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalCount / pageSize).ToString());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tickets");
                return StatusCode(500, "An error occurred while retrieving tickets");
            }
        }

        // GET: api/tickets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDetailDto>> GetTicket(int id)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                var isSupport = User.IsInRole("Support") || User.IsInRole("Admin");

                // Check if user has permission to view this ticket
                if (!isSupport && ticket.RequesterId != userId)
                {
                    return Forbid();
                }

                var result = new TicketDetailDto
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    Status = ticket.Status.ToString(),
                    Priority = ticket.Priority.ToString(),
                    Category = ticket.Category.ToString(),
                    CreatedAt = ticket.CreatedAt,
                    LastUpdatedAt = ticket.LastUpdatedAt,
                    ResolvedAt = ticket.ResolvedAt,
                    EstimatedResolutionTime = ticket.EstimatedResolutionTimeInMinutes,
                    ActualResolutionTime = ticket.ActualResolutionTimeInMinutes,
                    Requester = new UserDto
                    {
                        Id = ticket.RequesterId,
                        Name = $"{ticket.Requester.FirstName} {ticket.Requester.LastName}",
                        Email = ticket.Requester.Email,
                        Department = ticket.Requester.Department
                    },
                    AssignedTo = ticket.AssignedTo != null ? new UserDto
                    {
                        Id = ticket.AssignedToId,
                        Name = $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}",
                        Email = ticket.AssignedTo.Email,
                        Department = ticket.AssignedTo.Department
                    } : null,
                    Comments = ticket.Comments.Where(c => !c.IsInternal || isSupport)
                                     .OrderByDescending(c => c.CreatedAt)
                                     .Select(c => new CommentDto
                                     {
                                         Id = c.Id,
                                         Content = c.Content,
                                         CreatedAt = c.CreatedAt,
                                         IsInternal = c.IsInternal,
                                         Author = $"{c.Author.FirstName} {c.Author.LastName}"
                                     }).ToList(),
                    Attachments = ticket.Attachments.Select(a => new AttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        UploadedAt = a.UploadedAt,
                        FileSizeInBytes = a.FileSizeInBytes,
                        UploadedBy = $"{a.UploadedBy.FirstName} {a.UploadedBy.LastName}"
                    }).ToList(),
                    History = isSupport ? ticket.History
                                     .OrderByDescending(h => h.ChangedAt)
                                     .Select(h => new HistoryDto
                                     {
                                         Id = h.Id,
                                         Property = h.Property,
                                         OldValue = h.OldValue,
                                         NewValue = h.NewValue,
                                         ChangedAt = h.ChangedAt,
                                         ChangedBy = $"{h.ChangedBy.FirstName} {h.ChangedBy.LastName}"
                                     }).ToList() : new List<HistoryDto>()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving ticket with ID {id}");
                return StatusCode(500, "An error occurred while retrieving the ticket");
            }
        }

        // POST: api/tickets
        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketDto createTicketDto)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return Unauthorized();
                }

                var ticket = new Ticket
                {
                    Title = createTicketDto.Title,
                    Description = createTicketDto.Description,
                    Priority = Enum.Parse<TicketPriority>(createTicketDto.Priority),
                    Category = Enum.Parse<TicketCategory>(createTicketDto.Category),
                    Status = TicketStatus.New,
                    RequesterId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _ticketRepository.AddAsync(ticket);
                
                // Fetch the full ticket with related data
                ticket = await _ticketRepository.GetByIdAsync(ticket.Id);
                
                // Send email notification
                await _emailService.SendTicketCreatedAsync(ticket);

                return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, new TicketDto
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Status = ticket.Status.ToString(),
                    Priority = ticket.Priority.ToString(),
                    Category = ticket.Category.ToString(),
                    CreatedAt = ticket.CreatedAt,
                    RequesterName = $"{user.FirstName} {user.LastName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                return StatusCode(500, "An error occurred while creating the ticket");
            }
        }

        // PUT: api/tickets/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Support,Admin")]
        public async Task<IActionResult> UpdateTicket(int id, UpdateTicketDto updateTicketDto)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);

                // Track changes for history and notifications
                var changes = new List<(string Property, string OldValue, string NewValue)>();

                // Update fields if they're different from current values
                if (!string.Equals(ticket.Title, updateTicketDto.Title))
                {
                    changes.Add(("Title", ticket.Title, updateTicketDto.Title));
                    ticket.Title = updateTicketDto.Title;
                }

                if (!string.Equals(ticket.Description, updateTicketDto.Description))
                {
                    changes.Add(("Description", "Previous description", updateTicketDto.Description));
                    ticket.Description = updateTicketDto.Description;
                }

                if (Enum.TryParse<TicketPriority>(updateTicketDto.Priority, out var priority) && 
                    ticket.Priority != priority)
                {
                    changes.Add(("Priority", ticket.Priority.ToString(), priority.ToString()));
                    ticket.Priority = priority;
                }

                if (Enum.TryParse<TicketStatus>(updateTicketDto.Status, out var status) && 
                    ticket.Status != status)
                {
                    changes.Add(("Status", ticket.Status.ToString(), status.ToString()));
                    ticket.Status = status;

                    // Handle special status transitions
                    if (status == TicketStatus.Resolved && ticket.ResolvedAt == null)
                    {
                        ticket.ResolvedAt = DateTime.UtcNow;
                        
                        if (ticket.CreatedAt != null)
                        {
                            ticket.ActualResolutionTimeInMinutes = (int)(ticket.ResolvedAt.Value - ticket.CreatedAt).TotalMinutes;
                        }
                    }
                    
                    if (status == TicketStatus.Reopened)
                    {
                        ticket.ResolvedAt = null;
                        ticket.ActualResolutionTimeInMinutes = null;
                    }
                }

                if (!string.IsNullOrEmpty(updateTicketDto.AssignedToId) && 
                    ticket.AssignedToId != updateTicketDto.AssignedToId)
                {
                    var previousAssignee = ticket.AssignedTo != null 
                        ? $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}" 
                        : "Unassigned";
                        
                    var newAssignee = await _userManager.FindByIdAsync(updateTicketDto.AssignedToId);
                    var newAssigneeName = newAssignee != null 
                        ? $"{newAssignee.FirstName} {newAssignee.LastName}" 
                        : "Unknown";
                    
                    changes.Add(("AssignedTo", previousAssignee, newAssigneeName));
                    ticket.AssignedToId = updateTicketDto.AssignedToId;
                    
                    // If assigning for the first time, automatically change status to Assigned
                    if (ticket.Status == TicketStatus.New)
                    {
                        changes.Add(("Status", ticket.Status.ToString(), TicketStatus.Assigned.ToString()));
                        ticket.Status = TicketStatus.Assigned;
                    }
                }

                // If estimated resolution time changed
                if (updateTicketDto.EstimatedResolutionTimeInMinutes.HasValue && 
                    ticket.EstimatedResolutionTimeInMinutes != updateTicketDto.EstimatedResolutionTimeInMinutes)
                {
                    var oldValue = ticket.EstimatedResolutionTimeInMinutes.HasValue 
                        ? ticket.EstimatedResolutionTimeInMinutes.ToString() 
                        : "Unestimated";
                        
                    changes.Add(("EstimatedResolutionTime", oldValue, updateTicketDto.EstimatedResolutionTimeInMinutes.ToString()));
                    ticket.EstimatedResolutionTimeInMinutes = updateTicketDto.EstimatedResolutionTimeInMinutes;
                }

                // Update the ticket
                ticket.LastUpdatedAt = DateTime.UtcNow;
                await _ticketRepository.UpdateAsync(ticket);

                // Record history entries
                foreach (var change in changes)
                {
                    var history = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = change.Property,
                        OldValue = change.OldValue,
                        NewValue = change.NewValue,
                        ChangedAt = DateTime.UtcNow,
                        ChangedById = userId
                    };

                    ticket.History.Add(history);
                    
                    // Send email notification for important changes
                    if (change.Property != "Description") // Skip notifications for description changes
                    {
                        await _emailService.SendTicketUpdatedAsync(ticket, change.Property, change.OldValue, change.NewValue);
                    }
                }

                // Special notification for assignment changes
                if (changes.Any(c => c.Property == "AssignedTo") && !string.IsNullOrEmpty(ticket.AssignedToId))
                {
                    await _emailService.SendTicketAssignedAsync(ticket);
                }

                // Special notification for resolution
                if (changes.Any(c => c.Property == "Status" && c.NewValue == TicketStatus.Resolved.ToString()))
                {
                    await _emailService.SendTicketResolvedAsync(ticket);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating ticket with ID {id}");
                return StatusCode(500, "An error occurred while updating the ticket");
            }
        }

        // POST: api/tickets/5/comments
        [HttpPost("{id}/comments")]
        public async Task<ActionResult<CommentDto>> AddComment(int id, AddCommentDto commentDto)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                var user = await _userManager.FindByIdAsync(userId);
                
                // Regular users can only add comments to their own tickets
                if (!User.IsInRole("Support") && !User.IsInRole("Admin") && ticket.RequesterId != userId)
                {
                    return Forbid();
                }
                
                // Only support staff can add internal comments
                if (commentDto.IsInternal && !User.IsInRole("Support") && !User.IsInRole("Admin"))
                {
                    commentDto.IsInternal = false;
                }

                var comment = new TicketComment
                {
                    TicketId = id,
                    Content = commentDto.Content,
                    IsInternal = commentDto.IsInternal,
                    CreatedAt = DateTime.UtcNow,
                    AuthorId = userId
                };

                ticket.Comments.Add(comment);
                ticket.LastUpdatedAt = DateTime.UtcNow;
                
                // If a support user comments on a ticket and it's in New status, automatically move to In Progress
                if ((User.IsInRole("Support") || User.IsInRole("Admin")) && 
                    ticket.Status == TicketStatus.New || ticket.Status == TicketStatus.Assigned)
                {
                    ticket.Status = TicketStatus.InProgress;
                    
                    // Add status change to history
                    var history = new TicketHistory
                    {
                        TicketId = ticket.Id,
                        Property = "Status",
                        OldValue = ticket.Status.ToString(),
                        NewValue = TicketStatus.InProgress.ToString(),
                        ChangedAt = DateTime.UtcNow,
                        ChangedById = userId
                    };
                    ticket.History.Add(history);
                }

                await _ticketRepository.UpdateAsync(ticket);
                
                // Send notification email
                await _emailService.SendTicketCommentAddedAsync(ticket, comment);

                return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, new CommentDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    IsInternal = comment.IsInternal,
                    Author = $"{user.FirstName} {user.LastName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment to ticket with ID {id}");
                return StatusCode(500, "An error occurred while adding the comment");
            }
        }

        // POST: api/tickets/5/resolve
        [HttpPost("{id}/resolve")]
        [Authorize(Roles = "Support,Admin")]
        public async Task<IActionResult> ResolveTicket(int id, ResolveTicketDto resolveDto)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                
                // Check if the ticket is already resolved or closed
                if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
                {
                    return BadRequest("Ticket is already resolved or closed");
                }

                // Update ticket status
                var oldStatus = ticket.Status;
                ticket.Status = TicketStatus.Resolved;
                ticket.ResolvedAt = DateTime.UtcNow;
                ticket.LastUpdatedAt = DateTime.UtcNow;
                
                // Calculate resolution time
                if (ticket.CreatedAt != null)
                {
                    ticket.ActualResolutionTimeInMinutes = (int)(ticket.ResolvedAt.Value - ticket.CreatedAt).TotalMinutes;
                }

                // Add resolution note as a comment if provided
                if (!string.IsNullOrEmpty(resolveDto.ResolutionNote))
                {
                    var comment = new TicketComment
                    {
                        TicketId = id,
                        Content = resolveDto.ResolutionNote,
                        IsInternal = false,
                        CreatedAt = DateTime.UtcNow,
                        AuthorId = userId
                    };
                    ticket.Comments.Add(comment);
                }

                // Add history entry
                var history = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Property = "Status",
                    OldValue = oldStatus.ToString(),
                    NewValue = TicketStatus.Resolved.ToString(),
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = userId
                };
                ticket.History.Add(history);

                await _ticketRepository.UpdateAsync(ticket);
                
                // Send resolution email
                await _emailService.SendTicketResolvedAsync(ticket);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving ticket with ID {id}");
                return StatusCode(500, "An error occurred while resolving the ticket");
            }
        }

        // POST: api/tickets/5/close
        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseTicket(int id)
        {
            try
            {
                var ticket = await _ticketRepository.GetByIdAsync(id);
                if (ticket == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                
                // Only the requester or support staff can close a ticket
                if (ticket.RequesterId != userId && !User.IsInRole("Support") && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                // Ticket must be in Resolved status to be closed
                if (ticket.Status != TicketStatus.Resolved)
                {
                    return BadRequest("Only resolved tickets can be closed");
                }

                // Update ticket status
                var oldStatus = ticket.Status;
                ticket.Status = TicketStatus.Closed;
                ticket.LastUpdatedAt = DateTime.UtcNow;

                // Add history entry
                var history = new TicketHistory
                {
                    TicketId = ticket.Id,
                    Property = "Status",
                    OldValue = oldStatus.ToString(),
                    NewValue = TicketStatus.Closed.ToString(),
                    ChangedAt = DateTime.UtcNow,
                    ChangedById = userId
                };
                ticket.History.Add(history);

                await _ticketRepository.UpdateAsync(ticket);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error closing ticket with ID {id}");
                return StatusCode(500, "An error occurred while closing the ticket");
            }
        }
    }

    // DTO classes
    public class TicketDto
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

    public class TicketDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? EstimatedResolutionTime { get; set; }
        public int? ActualResolutionTime { get; set; }
        public UserDto Requester { get; set; }
        public UserDto AssignedTo { get; set; }
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
        public List<HistoryDto> History { get; set; } = new List<HistoryDto>();
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsInternal { get; set; }
        public string Author { get; set; }
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public long FileSizeInBytes { get; set; }
        public string UploadedBy { get; set; }
    }

    public class HistoryDto
    {
        public int Id { get; set; }
        public string Property { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; }
    }

    public class CreateTicketDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
    }

    public class UpdateTicketDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string AssignedToId { get; set; }
        public int? EstimatedResolutionTimeInMinutes { get; set; }
    }

    public class AddCommentDto
    {
        public string Content { get; set; }
        public bool IsInternal { get; set; }
    }

    public class ResolveTicketDto
    {
        public string ResolutionNote { get; set; }
    }
}
