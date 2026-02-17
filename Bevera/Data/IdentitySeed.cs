using Bevera.Models;
using Microsoft.AspNetCore.Identity;

namespace Bevera.Data
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            // ✅ RoleManager е за IdentityRole, НЕ за ApplicationUser
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Worker", "Client" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await EnsureUserWithRole(userManager, "admin@bevera.local", "Admin123!", "Admin");
            await EnsureUserWithRole(userManager, "worker@bevera.local", "Worker123!", "Worker");
            await EnsureUserWithRole(userManager, "client@bevera.local", "Client123!", "Client");
        }

        private static async Task EnsureUserWithRole(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    FirstName = "Test",
                    LastName = role,
                    Address = ""
                };

                var createResult = await userManager.CreateAsync(user, password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user {email}: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
