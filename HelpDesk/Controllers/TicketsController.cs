using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.Models.Enums;
using HelpDesk.Services;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITicketAssignmentService _assignmentService;
        private readonly ITicketMaintenanceService _maintenanceService;

        public TicketsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ITicketAssignmentService assignmentService,
            ITicketMaintenanceService maintenanceService)
        {
            _db = db;
            _userManager = userManager;
            _assignmentService = assignmentService;
            _maintenanceService = maintenanceService;
        }

        public async Task<IActionResult> Index()
        {
            await _maintenanceService.AutoCloseStaleResolvedTicketsAsync();

            var userId = _userManager.GetUserId(User);
            IQueryable<Ticket> query = _db.Tickets
                .Include(t => t.Client)
                .Include(t => t.Agent);

            if (User.IsInRole("Client"))
            {
                query = query.Where(t => t.ClientId == userId);
            }
            else if (User.IsInRole("Dispatcher"))
            {
                query = query.Where(t => (t.Status == TicketStatus.Open && t.Priority == null) || t.AgentId == userId);
            }
            else if (User.IsInRole("Agent"))
            {
                query = query.Where(t => t.AgentId == userId);
            }
            // no filter for admin

            var allTickets = await query
                .Select(t => new TicketListItemVM
                {
                    Id = t.Id,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    Department = t.Department,
                    ClientName = t.Client!.FullName,
                    AgentName = t.Agent != null ? t.Agent.FullName : null,
                    CreatedAt = t.CreatedAt,
                    DueDate = t.DueDate,
                    IsOverdue = t.DueDate.HasValue
                                && t.DueDate < DateTime.Now
                                && t.Status != TicketStatus.Resolved
                                && t.Status != TicketStatus.Closed
                })
                .ToListAsync();

            var vm = new TicketIndexVM
            {
                ActiveTickets = allTickets
                    .Where(t => t.Status != TicketStatus.Closed)
                    .OrderByDescending(t => t.Priority.HasValue ? (int)t.Priority.Value : -1)
                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                    .ToList(),

                ClosedTickets = allTickets
                    .Where(t => t.Status == TicketStatus.Closed)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create(TicketCreateVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var userId = _userManager.GetUserId(User);

            var ticket = new Ticket
            {
                Title = vm.Title,
                Description = vm.Description,
                ClientId = userId!,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.Now
            };

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _db.Tickets
                .Include(t => t.Client)
                .Include(t => t.Agent)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Client") && ticket.ClientId != userId) return Forbid();
            if (User.IsInRole("Agent") && ticket.AgentId != userId) return Forbid();

            bool isClient = User.IsInRole("Client");
            bool isAgentRole = User.IsInRole("Agent");
            bool isAdmin = User.IsInRole("Admin");
            bool isDispatcher = User.IsInRole("Dispatcher");
            bool isOwner = ticket.AgentId == userId;
            bool isAgent = isAgentRole && isOwner;
            bool agentCanComment = isAgent && ticket.Status != TicketStatus.Assigned;
            bool dispatcherCanComment = isDispatcher && isOwner && (ticket.Status == TicketStatus.InProgress || ticket.Status == TicketStatus.Resolved);

            if (isAgentRole && !isOwner) return Forbid();

            var visibleComments = ticket.Comments
                .Where(c => isClient ? !c.IsInternal : true)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentVM
                {
                    AuthorName = c.User!.FullName,
                    CommentText = c.CommentText,
                    IsInternal = c.IsInternal,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            var vm = new TicketDetailsVM
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                Department = ticket.Department,
                ClientName = ticket.Client!.FullName,
                AgentName = ticket.Agent?.FullName,
                CreatedAt = ticket.CreatedAt,
                DueDate = ticket.DueDate,
                IsOverdue = ticket.IsOverdue,
                Comments = visibleComments,

                CanComment = (isClient || agentCanComment || isAdmin || dispatcherCanComment) && ticket.Status != TicketStatus.Closed,
                CanPostInternal = isAgent || isAdmin || isDispatcher,
                CanStartProgress = (isAgentRole && isOwner && ticket.Status == TicketStatus.Assigned)
                     || (isAdmin && ticket.Status == TicketStatus.Assigned)
                     || (isDispatcher && ticket.Status == TicketStatus.Open && ticket.Priority == null && ticket.AgentId == null),
                CanResolve = isOwner && ticket.Status == TicketStatus.InProgress && (isAgentRole || isAdmin || isDispatcher),
                CanClose = isClient && ticket.Status == TicketStatus.Resolved
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Dispatcher,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            _db.Tickets.Remove(ticket);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Ticket deleted.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Dispatcher,Admin")]
        public async Task<IActionResult> Assign(int id, Department? department = null)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Status == TicketStatus.Closed) return BadRequest("Cannot assign a closed ticket.");

            bool isReassignment = ticket.AgentId != null;

            var effectiveDepartment = department ?? ticket.Department ?? Department.Hardware;

            var vm = new TicketAssignVM
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority ?? TicketPriority.Medium,
                Department = effectiveDepartment,
                IsReassignment = isReassignment,
                SelectedAgentId = department == null ? ticket.AgentId : null // clear selection if department changed
            };

            if (isReassignment)
            {
                var agentsWithLoad = await _assignmentService.GetAgentsWithLoadAsync(effectiveDepartment);
                vm.AvailableAgents = agentsWithLoad
                    .Select(x => new AgentOptionVM
                    {
                        Id = x.Agent.Id,
                        FullName = x.Agent.FullName,
                        ActiveTicketCount = x.ActiveTicketCount
                    })
                    .ToList();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Dispatcher,Admin")]
        public async Task<IActionResult> Assign(TicketAssignVM vm)
        {
            var ticket = await _db.Tickets.FindAsync(vm.TicketId);
            if (ticket == null) return NotFound();

            if (ticket.Status == TicketStatus.Closed) return BadRequest("Cannot assign a closed ticket.");

            bool willUseManualAgent = !string.IsNullOrEmpty(vm.SelectedAgentId);

            if (vm.IsReassignment && !willUseManualAgent)
            {
                ModelState.AddModelError("", "No agents available in this department. Choose a different department.");
            }
            else if (!vm.IsReassignment)
            {
                // First-time assign: check up front whether any agent exists in this department
                var candidateAgent = await _assignmentService.FindAgentForAssignment(vm.Department);
                if (candidateAgent == null)
                {
                    ModelState.AddModelError("", "No agents available in this department. Choose a different department.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (ticket.AgentId != null || vm.IsReassignment)
                {
                    var agentsWithLoad = await _assignmentService.GetAgentsWithLoadAsync(vm.Department);
                    vm.AvailableAgents = agentsWithLoad
                        .Select(x => new AgentOptionVM { Id = x.Agent.Id, FullName = x.Agent.FullName, ActiveTicketCount = x.ActiveTicketCount })
                        .ToList();
                    vm.IsReassignment = true;
                }
                return View(vm);
            }

            if (willUseManualAgent)
            {
                await _assignmentService.AssignTicketToAgentAsync(ticket, vm.Priority, vm.Department, vm.SelectedAgentId!);
            }
            else
            {
                await _assignmentService.AssignTicketAsync(ticket, vm.Priority, vm.Department);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CommentCreateVM vm)
        {
            var ticket = await _db.Tickets.FindAsync(vm.TicketId);
            if (ticket == null) return NotFound();

            if (ticket.Status == TicketStatus.Closed) return BadRequest("Cannot comment on a closed ticket.");

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Client") && ticket.ClientId != userId) return Forbid();
            if (User.IsInRole("Agent") && ticket.AgentId != userId) return Forbid();

            if (User.IsInRole("Agent") && ticket.Status == TicketStatus.Assigned)
                return BadRequest("Start progress on this ticket before commenting.");

            if (User.IsInRole("Dispatcher") &&
                (ticket.AgentId != userId || (ticket.Status != TicketStatus.InProgress && ticket.Status != TicketStatus.Resolved)))
            {
                return BadRequest("You can only comment on tickets you own while they are in progress or resolved.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Comment could not be added. Please check your input.";
                return RedirectToAction(nameof(Details), new { id = vm.TicketId });
            }

            bool isInternal = vm.IsInternal && !User.IsInRole("Client");

            var comment = new TicketComment
            {
                TicketId = vm.TicketId,
                UserId = userId!,
                CommentText = vm.CommentText,
                IsInternal = isInternal,
                CreatedAt = DateTime.Now
            };

            _db.TicketComments.Add(comment);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = vm.TicketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Agent,Admin,Dispatcher")]
        public async Task<IActionResult> StartProgress(int id)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Agent"))
            {
                if (ticket.AgentId != userId) return Forbid();
                if (ticket.Status != TicketStatus.Assigned) return BadRequest();
                ticket.Status = TicketStatus.InProgress;
            }
            else if (User.IsInRole("Admin"))
            {
                if (ticket.Status != TicketStatus.Assigned) return BadRequest();
                ticket.AgentId = userId; // admin takes over ownership
                ticket.Status = TicketStatus.InProgress;
            }
            else if (User.IsInRole("Dispatcher"))
            {
                // Dispatcher can only take over a ticket still sitting untriaged in their queue
                if (ticket.Status != TicketStatus.Open || ticket.Priority != null || ticket.AgentId != null)
                    return BadRequest();

                ticket.AgentId = userId;
                ticket.Status = TicketStatus.InProgress; 
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Agent,Admin,Dispatcher")]
        public async Task<IActionResult> Resolve(int id)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (ticket.Status != TicketStatus.InProgress) return BadRequest();
            if (ticket.AgentId != userId) return Forbid(); // must currently own it, regardless of role

            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var ticket = await _db.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (ticket.ClientId != userId) return Forbid();

            if (ticket.Status != TicketStatus.Resolved) return BadRequest();

            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}