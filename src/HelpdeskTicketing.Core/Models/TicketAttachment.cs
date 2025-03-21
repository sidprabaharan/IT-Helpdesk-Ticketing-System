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
