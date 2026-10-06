using HelpDesk.Data;
using HelpDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Services
{
    public class TicketMaintenanceService : ITicketMaintenanceService
    {
        private readonly ApplicationDbContext _db;

        public TicketMaintenanceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AutoCloseStaleResolvedTicketsAsync()
        {
            var cutoff = DateTime.Now.AddDays(-3);

            var staleTickets = await _db.Tickets
                .Where(t => t.Status == TicketStatus.Resolved
                         && t.ResolvedAt != null
                         && t.ResolvedAt < cutoff)
                .ToListAsync();

            if (!staleTickets.Any()) return;

            foreach (var ticket in staleTickets)
            {
                ticket.Status = TicketStatus.Closed;
                ticket.ClosedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
        }
    }
}