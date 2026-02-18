using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRolesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminRolesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
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

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new UserRoleRowViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                RoleName = roles.FirstOrDefault()
            };

            ViewBag.CreatedAt = user.CreatedAt;
            return View(vm);
        }

        // =========================
        // EDIT ROLE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();

            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync();

            var vm = new EditUserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                RoleName = currentRole,
                Roles = roles
            };

            ViewBag.IsSelf = (_userManager.GetUserId(User) == user.Id);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserRoleViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.UserId)) return NotFound();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.UserId);
            if (user == null) return NotFound();

            var isSelf = (_userManager.GetUserId(User) == user.Id);

            // ⚠️ админ НЕ може да маха Admin от себе си
            if (isSelf && !string.Equals(vm.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.RoleName), "Като Admin не можеш да промениш собствената си роля.");
            }

            // role must exist (unless empty)
            if (!string.IsNullOrWhiteSpace(vm.RoleName))
            {
                var exists = await _roleManager.RoleExistsAsync(vm.RoleName);
                if (!exists)
                    ModelState.AddModelError(nameof(vm.RoleName), "Невалидна роля.");
            }

            if (!ModelState.IsValid)
            {
                vm.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new SelectListItem(r.Name!, r.Name!))
                    .ToListAsync();

                ViewBag.IsSelf = isSelf;
                return View(vm);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(vm.RoleName))
                await _userManager.AddToRoleAsync(user, vm.RoleName);

            TempData["FlashMessage"] = "Ролята е променена.";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CREATE USER
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync();

            var vm = new CreateUserViewModel { Roles = roles, RoleName = "Client" };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.RoleName))
            {
                var exists = await _roleManager.RoleExistsAsync(vm.RoleName);
                if (!exists)
                    ModelState.AddModelError(nameof(vm.RoleName), "Невалидна роля.");
            }

            if (!ModelState.IsValid)
            {
                vm.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new SelectListItem(r.Name!, r.Name!))
                    .ToListAsync();
                return View(vm);
            }

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                FirstName = vm.FirstName?.Trim(),
                LastName = vm.LastName?.Trim(),
                PhoneNumber = vm.PhoneNumber?.Trim(),
                Address = vm.Address?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                vm.Roles = await _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new SelectListItem(r.Name!, r.Name!))
                    .ToListAsync();
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.RoleName))
                await _userManager.AddToRoleAsync(user, vm.RoleName);

            TempData["FlashMessage"] = "Потребителят е създаден.";
            TempData["FlashType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE USER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var currentId = _userManager.GetUserId(User);
            if (currentId == id)
            {
                TempData["FlashMessage"] = "Не можеш да изтриеш собствения си акаунт.";
                TempData["FlashType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["FlashMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                TempData["FlashType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            TempData["FlashMessage"] = "Потребителят е изтрит.";
            TempData["FlashType"] = "success";
            return RedirectToAction(nameof(Index));
        }
    }
}
