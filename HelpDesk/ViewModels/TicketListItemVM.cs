using HelpDesk.Models.Enums;

namespace HelpDesk.ViewModels
{
    public class TicketListItemVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public Department? Department { get; set; }
        public string? ClientName { get; set; }
        public string? AgentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}