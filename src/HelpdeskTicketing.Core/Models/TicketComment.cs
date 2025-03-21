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
