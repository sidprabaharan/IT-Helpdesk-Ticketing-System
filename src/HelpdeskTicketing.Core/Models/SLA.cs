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
