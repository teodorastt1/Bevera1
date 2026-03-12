using Bevera.Models;
using Microsoft.AspNetCore.Identity;

namespace Bevera.Data
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Worker", "Client", "Distributor" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create role {role}: {errors}");
                    }
                }
            }

            await EnsureUserWithRole(
                userManager,
                "admin@bevera.local",
                "Admin123!",
                "Admin",
                "Система",
                "Администратор");

            await EnsureUserWithRole(
                userManager,
                "worker@bevera.local",
                "Worker123!",
                "Worker",
                "Склад",
                "Служител");

            await EnsureUserWithRole(
                userManager,
                "client@bevera.local",
                "Client123!",
                "Client",
                "Тестов",
                "Клиент");

            await EnsureUserWithRole(
                userManager,
                "distributor@bevera.local",
                "Distributor123!",
                "Distributor",
                "Тестов",
                "Дистрибутор");
        }

        private static async Task EnsureUserWithRole(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role,
            string firstName,
            string lastName)
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
                    FirstName = firstName,
                    LastName = lastName,
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
                var roleResult = await userManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to add role {role} to user {email}: {errors}");
                }
            }
        }
    }
}