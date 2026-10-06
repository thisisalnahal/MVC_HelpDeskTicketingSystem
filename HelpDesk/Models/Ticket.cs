using HelpDesk.Models.Enums;

namespace HelpDesk.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority? Priority { get; set; }
        public Department? Department { get; set; }

        public string ClientId { get; set; } = string.Empty;
        public ApplicationUser? Client { get; set; }

        public string? AgentId { get; set; }
        public ApplicationUser? Agent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }

        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

        public bool IsOverdue =>
            DueDate.HasValue
            && DueDate.Value < DateTime.Now
            && Status != TicketStatus.Resolved
            && Status != TicketStatus.Closed;
    }
}