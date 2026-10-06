using System.ComponentModel.DataAnnotations;

namespace HelpDesk.ViewModels
{
    public class CommentCreateVM
    {
        [Required]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Comment cannot be empty.")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment must be under 1000 characters.")]
        public string CommentText { get; set; } = string.Empty;

        public bool IsInternal { get; set; } = false;
    }
}