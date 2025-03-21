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
