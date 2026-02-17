using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRolesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminRolesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? q, string? role, int page = 1, int pageSize = 5)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;

            // 1) взимаме всички users (по желание: AsNoTracking)
            var usersQuery = _db.Users.AsNoTracking().AsQueryable();

            // 2) search filter
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.Email ?? "").ToLower().Contains(q) ||
                    (u.FirstName ?? "").ToLower().Contains(q) ||
                    (u.LastName ?? "").ToLower().Contains(q) ||
                    (u.PhoneNumber ?? "").ToLower().Contains(q) ||
                    (u.Address ?? "").ToLower().Contains(q));
            }

            // 3) role filter (ще филтрираме по role чрез join към AspNetUserRoles)
            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleId = await _db.Roles
                    .Where(r => r.Name == role)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(roleId))
                {
                    usersQuery = usersQuery.Where(u =>
                        _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
                }
                else
                {
                    // ако role не съществува -> празен резултат
                    usersQuery = usersQuery.Where(u => false);
                }
            }

            // 4) total count (за pagination)
            var totalItems = await usersQuery.CountAsync();

            // 5) paging
            var usersPage = await usersQuery
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 6) мапваме към UserRoleRowViewModel + взимаме RoleName
            var rows = new List<UserRoleRowViewModel>();

            foreach (var u in usersPage)
            {
                var roles = await _userManager.GetRolesAsync(u);
                rows.Add(new UserRoleRowViewModel
                {
                    UserId = u.Id,
                    Email = u.Email ?? "",
                    FirstName = u.FirstName ?? "",
                    LastName = u.LastName ?? "",
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    RoleName = roles.FirstOrDefault()
                });
            }

            var model = new PagedResult<UserRoleRowViewModel>
            {
                Items = rows,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            ViewBag.Q = q;
            ViewBag.Role = role;
            ViewBag.PageSize = pageSize;

            return View(model);
        }
    }
}
