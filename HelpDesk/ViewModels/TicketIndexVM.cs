namespace HelpDesk.ViewModels
{
    public class TicketIndexVM
    {
        public List<TicketListItemVM> ActiveTickets { get; set; } = new();
        public List<TicketListItemVM> ClosedTickets { get; set; } = new();
    }
}