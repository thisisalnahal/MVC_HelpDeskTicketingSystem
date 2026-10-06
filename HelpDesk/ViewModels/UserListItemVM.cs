using HelpDesk.Models.Enums;

namespace HelpDesk.ViewModels
{
    public class UserListItemVM
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Department? Department { get; set; }
    }
}