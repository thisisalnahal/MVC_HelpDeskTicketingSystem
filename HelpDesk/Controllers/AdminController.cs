using HelpDesk.Data;
using HelpDesk.Models;
using HelpDesk.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult CreateStaff() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(CreateStaffVM vm)
        {
            if (vm.Role != "Agent" && vm.Role != "Dispatcher")
            {
                ModelState.AddModelError("", "Invalid role selected.");
            }

            if (vm.Role == "Agent" && vm.Department == null)
            {
                ModelState.AddModelError("", "Department is required for Agent accounts.");
            }

            if (!ModelState.IsValid) return View(vm);

            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                EmailConfirmed = true,
                Department = vm.Role == "Agent" ? vm.Department : null,
                LastAssignedAt = vm.Role == "Agent" ? DateTime.Now : null
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, vm.Role);
            TempData["Success"] = $"{vm.Role} {vm.FullName} created.";
            return RedirectToAction(nameof(CreateStaff));
        }

        [HttpGet]
        public async Task<IActionResult> Users(string role = "Client")
        {
            if (role != "Client" && role != "Agent" && role != "Dispatcher" && role != "Admin")
                role = "Client";

            var usersInRole = await _userManager.GetUsersInRoleAsync(role);

            var vm = usersInRole
                .Select(u => new UserListItemVM
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    Department = u.Department
                })
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.SelectedRole = role;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users), new { role });
            }

            bool hasTickets = await _db.Tickets.AnyAsync(t => t.ClientId == user.Id || t.AgentId == user.Id);
            if (hasTickets)
            {
                TempData["Error"] = $"Cannot delete {user.FullName}: they have tickets linked to their account.";
                return RedirectToAction(nameof(Users), new { role });
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"{user.FullName} deleted.";
            return RedirectToAction(nameof(Users), new { role });
        }
    }
}