using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Services
{
    public class TicketAssignmentService : ITicketAssignmentService
    {
        private readonly ApplicationDbContext _db;

        public TicketAssignmentService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task<DateTime> CalculateDueDate(TicketPriority priority)
        {
            DateTime due = priority switch
            {
                TicketPriority.Urgent => DateTime.Now.AddHours(4),
                TicketPriority.High => DateTime.Now.AddHours(24),
                TicketPriority.Medium => DateTime.Now.AddHours(48),
                TicketPriority.Low => DateTime.Now.AddHours(72),
                _ => DateTime.Now.AddHours(48)
            };

            return Task.FromResult(due);
        }

        public async Task<ApplicationUser?> FindAgentForAssignment(Department department)
        {
            var agentsInDept = await _db.Users
                .Where(u => u.Department == department)
                .ToListAsync();

            if (!agentsInDept.Any()) return null;

            var agentIds = agentsInDept.Select(a => a.Id).ToList();

            var activeCounts = await _db.Tickets
                .Where(t => t.AgentId != null
                         && agentIds.Contains(t.AgentId)
                         && t.Status != TicketStatus.Resolved
                         && t.Status != TicketStatus.Closed)
                .GroupBy(t => t.AgentId)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToListAsync();

            var bestAgent = agentsInDept
                .Select(a => new
                {
                    Agent = a,
                    ActiveTickets = activeCounts.FirstOrDefault(c => c.AgentId == a.Id)?.Count ?? 0
                })
                .OrderBy(x => x.ActiveTickets)
                .ThenBy(x => x.Agent.LastAssignedAt)
                .FirstOrDefault();

            return bestAgent?.Agent;
        }

        public async Task AssignTicketAsync(Ticket ticket, TicketPriority priority, Department department)
        {
            var agent = await FindAgentForAssignment(department);
            if (agent == null)
            {
                ticket.Priority = priority;
                ticket.Department = department;
                ticket.DueDate = await CalculateDueDate(priority);
                return;
            }

            ticket.Priority = priority;
            ticket.Department = department;
            ticket.DueDate = await CalculateDueDate(priority);
            ticket.AgentId = agent.Id;
            ticket.Status = TicketStatus.Assigned;

            agent.LastAssignedAt = DateTime.Now;
            _db.Users.Update(agent);
        }

        public async Task AssignTicketToAgentAsync(Ticket ticket, TicketPriority priority, Department department, string agentId)
        {
            var agent = await _db.Users.FirstOrDefaultAsync(u => u.Id == agentId && u.Department == department);
            if (agent == null)
                throw new InvalidOperationException("Selected agent not found in the specified department.");

            ticket.Priority = priority;
            ticket.Department = department;
            ticket.DueDate = await CalculateDueDate(priority);
            ticket.AgentId = agent.Id;
            ticket.Status = TicketStatus.Assigned;

            agent.LastAssignedAt = DateTime.Now;
            _db.Users.Update(agent);
        }

        public async Task<List<(ApplicationUser Agent, int ActiveTicketCount)>> GetAgentsWithLoadAsync(Department department)
        {
            var agentsInDept = await _db.Users
                .Where(u => u.Department == department)
                .ToListAsync();

            if (!agentsInDept.Any()) return new();

            var agentIds = agentsInDept.Select(a => a.Id).ToList();

            var activeCounts = await _db.Tickets
                .Where(t => t.AgentId != null
                         && agentIds.Contains(t.AgentId)
                         && t.Status != TicketStatus.Resolved
                         && t.Status != TicketStatus.Closed)
                .GroupBy(t => t.AgentId)
                .Select(g => new { AgentId = g.Key, Count = g.Count() })
                .ToListAsync();

            return agentsInDept
                .Select(a => (a, activeCounts.FirstOrDefault(c => c.AgentId == a.Id)?.Count ?? 0))
                .OrderBy(x => x.Item2)
                .ToList();
        }
    }
}