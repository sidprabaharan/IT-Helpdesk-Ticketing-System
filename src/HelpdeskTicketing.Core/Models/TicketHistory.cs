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
