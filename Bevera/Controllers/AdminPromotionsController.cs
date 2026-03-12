using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminPromotionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? q = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q));

            var products = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.Q = q;
            return View(products);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new AdminPromotionFormViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                DiscountEndsAt = product.DiscountEndsAt.HasValue
                    ? DateTime.SpecifyKind(product.DiscountEndsAt.Value, DateTimeKind.Utc).ToLocalTime()
                    : null
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminPromotionFormViewModel vm)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == vm.ProductId);
            if (product == null) return NotFound();

            var hasPercent = vm.DiscountPercent.HasValue && vm.DiscountPercent.Value > 0;
            var hasEndDate = vm.DiscountEndsAt.HasValue;

            if (hasPercent && !hasEndDate)
                ModelState.AddModelError(nameof(vm.DiscountEndsAt), "Избери крайна дата.");

            if (!hasPercent && hasEndDate)
                ModelState.AddModelError(nameof(vm.DiscountPercent), "Въведи процент намаление.");

            if (hasPercent && hasEndDate && vm.DiscountEndsAt!.Value <= DateTime.Now)
                ModelState.AddModelError(nameof(vm.DiscountEndsAt), "Крайната дата трябва да е в бъдещето.");

            if (!ModelState.IsValid)
                return View(vm);

            var hadPromoBefore = product.DiscountPercent.HasValue &&
                                 product.DiscountPercent.Value > 0 &&
                                 (!product.DiscountEndsAt.HasValue || product.DiscountEndsAt.Value >= DateTime.UtcNow);

            if (!hasPercent)
            {
                product.DiscountPercent = null;
                product.DiscountEndsAt = null;
            }
            else
            {
                product.DiscountPercent = vm.DiscountPercent;
                product.DiscountEndsAt = DateTime.SpecifyKind(vm.DiscountEndsAt!.Value, DateTimeKind.Local).ToUniversalTime();
            }

            await _context.SaveChangesAsync();

            var hasPromoNow = product.DiscountPercent.HasValue &&
                              product.DiscountPercent.Value > 0 &&
                              (!product.DiscountEndsAt.HasValue || product.DiscountEndsAt.Value >= DateTime.UtcNow);

            // Пращаме нотификации само ако има активна промоция след save
            if (hasPromoNow)
            {
                var clients = await _userManager.GetUsersInRoleAsync("Client");

                foreach (var client in clients)
                {
                    _context.AppNotifications.Add(new AppNotification
                    {
                        UserId = client.Id,
                        Message = $"Нова промоция: {product.Name} е с -{product.DiscountPercent?.ToString("0")}%.",
                        Type = "Promo",
                        Url = $"/Client/Product/{product.Id}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }

            TempData["ToastMessage"] = hasPromoNow
                ? "Промоцията е запазена успешно."
                : "Промоцията е премахната успешно.";

            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }
    }
}