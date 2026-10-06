using Microsoft.AspNetCore.Identity;
using HelpDesk.Models.Enums;

namespace HelpDesk.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public Department? Department { get; set; }

        // (for tie-breaking)
        public DateTime? LastAssignedAt { get; set; }
    }
}