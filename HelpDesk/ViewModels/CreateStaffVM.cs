using System.ComponentModel.DataAnnotations;
using HelpDesk.Models.Enums;

namespace HelpDesk.ViewModels
{
    public class CreateStaffVM
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Agent"; // "Agent" or "Dispatcher"

        // Only required when Role == Agent
        public Department? Department { get; set; }
    }
}