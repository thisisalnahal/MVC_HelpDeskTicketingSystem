using System.ComponentModel.DataAnnotations;
using HelpDesk.Models.Enums;

namespace HelpDesk.ViewModels
{
    public class TicketAssignVM
    {
        public int TicketId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Required]
        public TicketPriority Priority { get; set; }

        [Required]
        public Department Department { get; set; }

        // If set, skip auto load-balancing and assign directly to this agent
        public string? SelectedAgentId { get; set; }

        public List<AgentOptionVM> AvailableAgents { get; set; } = new();

        public bool IsReassignment { get; set; }
    }

    public class AgentOptionVM
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int ActiveTicketCount { get; set; }
    }
}