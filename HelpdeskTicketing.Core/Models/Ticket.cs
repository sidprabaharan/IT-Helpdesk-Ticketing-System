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

// HelpdeskTicketing.Core/Models/TicketComment.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsInternal { get; set; } = false;

        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }

        public string AuthorId { get; set; }
        public virtual ApplicationUser Author { get; set; }
    }
}

// HelpdeskTicketing.Core/Models/TicketAttachment.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        public string StoragePath { get; set; }

        public string ContentType { get; set; }
        
        public long FileSizeInBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }

        public string UploadedById { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; }
    }
}

// HelpdeskTicketing.Core/Models/TicketHistory.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class TicketHistory
    {
        public int Id { get; set; }

        [Required]
        public string Property { get; set; }

        public string OldValue { get; set; }
        
        public string NewValue { get; set; }
        
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int TicketId { get; set; }
        public virtual Ticket Ticket { get; set; }

        public string ChangedById { get; set; }
        public virtual ApplicationUser ChangedBy { get; set; }
    }
}

// HelpdeskTicketing.Core/Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace HelpdeskTicketing.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }

        // Navigation properties
        public virtual ICollection<Ticket> CreatedTickets { get; set; }
        public virtual ICollection<Ticket> AssignedTickets { get; set; }
        public virtual ICollection<TicketComment> Comments { get; set; }
        public virtual ICollection<TicketAttachment> Uploads { get; set; }
        public virtual ICollection<TicketHistory> Changes { get; set; }
    }
}

// HelpdeskTicketing.Core/Models/Team.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class Team
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
        public virtual ICollection<TeamCategory> Categories { get; set; } = new List<TeamCategory>();
    }

    public class TeamMember
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public bool IsTeamLead { get; set; } = false;
    }

    public class TeamCategory
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public virtual Team Team { get; set; }
        public TicketCategory Category { get; set; }
    }
}

// HelpdeskTicketing.Core/Models/SLA.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HelpdeskTicketing.Core.Models
{
    public class SLA
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public TicketPriority Priority { get; set; }
        
        public TicketCategory? Category { get; set; }
        
        public int ResponseTimeInMinutes { get; set; }
        
        public int ResolutionTimeInMinutes { get; set; }
        
        public bool IsDefault { get; set; } = false;
    }
}
