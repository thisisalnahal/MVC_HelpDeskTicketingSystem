namespace HelpDesk.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public string CommentText { get; set; } = string.Empty;
        public bool IsInternal { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}