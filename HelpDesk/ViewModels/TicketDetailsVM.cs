using HelpDesk.Models.Enums;

namespace HelpDesk.ViewModels
{
    public class TicketDetailsVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public Department? Department { get; set; }
        public string? ClientName { get; set; }
        public string? AgentName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }

        public List<CommentVM> Comments { get; set; } = new();

        // Flags the view uses to decide which action buttons/forms to show
        public bool CanComment { get; set; }
        public bool CanPostInternal { get; set; }
        public bool CanStartProgress { get; set; }
        public bool CanResolve { get; set; }
        public bool CanClose { get; set; }
    }

    public class CommentVM
    {
        public string AuthorName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}