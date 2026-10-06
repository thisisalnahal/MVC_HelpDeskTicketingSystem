using System.ComponentModel.DataAnnotations;

namespace HelpDesk.ViewModels
{
    public class TicketCreateVM
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters.")]
        public string Description { get; set; } = string.Empty;
    }
}