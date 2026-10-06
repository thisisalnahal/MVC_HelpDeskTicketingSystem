using Microsoft.AspNetCore.Identity;
using HelpDesk.Models;
using HelpDesk.Models.Enums;

namespace HelpDesk.Data
{
    public static class DbInitializer
    {
        public static readonly string[] Roles = { "Client", "Dispatcher", "Agent", "Admin" };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await SeedUserAsync(userManager, "admin@helpdesk.com", "Admin123!", "System Admin", "Admin", null);
            await SeedUserAsync(userManager, "dispatcher@helpdesk.com", "Dispatch123!", "Default Dispatcher", "Dispatcher", null);

            await SeedUserAsync(userManager, "agent.hw1@helpdesk.com", "Agent123!", "Hardware Agent 1", "Agent", Department.Hardware);
            await SeedUserAsync(userManager, "agent.hw2@helpdesk.com", "Agent123!", "Hardware Agent 2", "Agent", Department.Hardware);

            await SeedUserAsync(userManager, "agent.sw1@helpdesk.com", "Agent123!", "Software Agent 1", "Agent", Department.Software);
        }

        private static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string role,
            Department? department)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                Department = department,
                LastAssignedAt = role == "Agent" ? DateTime.Now : null
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}