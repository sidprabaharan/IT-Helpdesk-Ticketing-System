using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        public DateTime? LastUpdatedAt { get; set; }

        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        [Required]
        public TicketStatus Status { get; set; } = TicketStatus.New;

        [Required]
        public TicketCategory Category { get; set; }

        public string RequesterId { get; set; }
        public virtual ApplicationUser Requester { get; set; }

        public string? AssignedToId { get; set; }
        public virtual ApplicationUser AssignedTo { get; set; }

        public int? EstimatedResolutionTimeInMinutes { get; set; }
        public int? ActualResolutionTimeInMinutes { get; set; }

        public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
        public virtual ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
    }

    public enum TicketStatus
    {
        New,
        Assigned,
        InProgress,
        OnHold,
        Resolved,
        Closed,
        Reopened
    }

    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TicketCategory
    {
        Hardware,
        Software,
        Network,
        Account,
        Email,
        Other
    }
}
