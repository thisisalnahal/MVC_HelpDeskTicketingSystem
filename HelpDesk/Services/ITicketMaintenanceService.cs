namespace HelpDesk.Services
{
    public interface ITicketMaintenanceService
    {
        Task AutoCloseStaleResolvedTicketsAsync();
    }
}