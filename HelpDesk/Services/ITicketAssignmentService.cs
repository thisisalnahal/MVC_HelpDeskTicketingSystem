using HelpDesk.Models;
using HelpDesk.Models.Enums;

namespace HelpDesk.Services
{
    public interface ITicketAssignmentService
    {
        Task<DateTime> CalculateDueDate(TicketPriority priority);
        Task<ApplicationUser?> FindAgentForAssignment(Department department);
        Task AssignTicketAsync(Ticket ticket, TicketPriority priority, Department department);
        Task AssignTicketToAgentAsync(Ticket ticket, TicketPriority priority, Department department, string agentId);
        Task<List<(ApplicationUser Agent, int ActiveTicketCount)>> GetAgentsWithLoadAsync(Department department);
    }
}