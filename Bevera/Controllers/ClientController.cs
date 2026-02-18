using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Bevera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace Bevera.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InvoiceService _invoiceService;

        public ClientController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, InvoiceService invoiceService)
        {
            _db = db;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        // /Client/Category/8  (+ filters)
        [HttpGet]
        public async Task<IActionResult> Category(int id, string? q, decimal? minPrice, decimal? maxPrice, bool onlyAvailable = false)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (category == null) return NotFound();

            // If this is a parent category, show its subcategories (Bai Iliya style tiles)
            var subcats = await _db.Categories
                .Where(c => c.IsActive && c.ParentCategoryId == id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (subcats.Count > 0)
            {
                ViewBag.CategoryName = category.Name;
                ViewBag.ParentId = category.Id;
                return View("CategoryParent", subcats);
            }

            var productsQuery = _db.Products
                .Where(p => p.CategoryId == id && p.IsActive)
                .Include(p => p.Images)
                .OrderBy(p => p.Name)
                .AsQueryable();

            // search
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                productsQuery = productsQuery.Where(p => p.Name.Contains(q));
            }

            // price range
            if (minPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

            // only available (не показваме колко, само филтър)
            if (onlyAvailable)
                productsQuery = productsQuery.Where(p => (p.Quantity > 0) || (p.StockQty > 0));

            var products = await productsQuery.ToListAsync();

            // за да останат стойностите във филтрите след submit
            ViewBag.CategoryName = category.Name;
            ViewBag.Q = q;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.OnlyAvailable = onlyAvailable;

            // за сърчицата (ако не си логната -> празно)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favIds = new List<int>();

            if (!string.IsNullOrEmpty(userId))
            {
                favIds = await _db.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ProductId)
                    .ToListAsync();
            }

            ViewBag.FavIds = favIds;

            return View(products);
        }

        // /Client/Product/5
        [HttpGet]
        public async Task<IActionResult> Product(int id)
        {
            var p = await _db.Products
                .Include(x => x.Images)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (p == null) return NotFound();
            return View(p); // Views/Client/Product.cshtml
        }

        // Search from navbar
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            q = q?.Trim();

            ViewBag.Q = q;

            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                // празно търсене -> не показваме всичко, само празен резултат
                return View(new List<Bevera.Models.Catalog.Product>());
            }

            // basic search in Name (and optionally Description)
            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)))
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);
            if (order == null) return NotFound();

            if (order.Status == OrderStates.Delivered)
            {
                order.Status = OrderStates.Received;
                order.ChangedAt = DateTime.UtcNow;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = OrderStates.Received,
                    Note = "Клиентът потвърди получаване.",
                    ChangedAt = DateTime.UtcNow,
                    ChangedByUserId = userId!
                });

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Profile));
        }

        // Client invoice download (only own orders)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);
            if (order == null) return NotFound();

            if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
            {
                await _invoiceService.GenerateInvoiceAsync(order.Id);
                order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);
                if (order == null) return NotFound();
            }

            if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
                return StatusCode(500, "Неуспешно генериране на фактура.");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "invoices", order.InvoiceStoredFileName);
            if (!System.IO.File.Exists(path))
                return NotFound("Фактурата липсва на сървъра.");

            return PhysicalFile(path, order.InvoiceContentType ?? "application/pdf", order.InvoiceFileName ?? $"Invoice_{order.Id}.pdf");
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vm = new ClientProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Orders = new List<OrderRowVm>()
            };

            if (_db.Orders != null)
            {
                vm.Orders = await _db.Orders
                    .Where(o => o.ClientId == user.Id)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new OrderRowVm
                    {
                        OrderId = o.Id,
                        CreatedAt = o.CreatedAt,
                        Total = o.Total,
                        Status = o.Status,
                        HasInvoice = o.InvoiceStoredFileName != null
                    })
                    .ToListAsync();
            }

            return View(vm); // Views/Client/Profile.cshtml
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Index", "Home");

            var favorites = await _db.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(favorites); // Views/Client/Favorites.cshtml
        }

        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new EditProfileVm
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? ""
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = vm.FirstName.Trim();
            user.LastName = vm.LastName.Trim();
            user.PhoneNumber = vm.PhoneNumber.Trim();
            user.Address = vm.Address.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Неуспешно запазване. Опитай пак.");
                return View(vm);
            }

            TempData["Msg"] = "Данните са обновени успешно.";
            return RedirectToAction(nameof(Profile));
        }


        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Index", "Home");

            var existing = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existing == null)
            {
                _db.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
                TempData["FlashMessage"] = "Добавено в Любими.";
                TempData["FlashType"] = "success";
            }
            else
            {
                _db.Favorites.Remove(existing);
                TempData["FlashMessage"] = "Премахнато от Любими.";
                TempData["FlashType"] = "warning";
            }

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Favorites));
        }

        // GET: /Client/OrderDetails/5
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId); // <-- смени ClientUserId с твоето поле

            if (order == null)
                return NotFound();

            var vm = new OrderDetailsViewModel
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                Status = order.Status.ToString(),
                Items = order.Items.Select(oi => new OrderDetailsItemViewModel
                {
                    ProductId = oi.ProductId,
                    Name = oi.Product.Name,
                    ImageUrl = oi.Product.Images
                        .OrderByDescending(img => img.IsMain)
                        .ThenBy(img => img.Id)
                        .Select(img => img.ImagePath)
                        .FirstOrDefault()
                        ?? "/img/no-image.png",
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity
                }).ToList()
            };

            vm.Total = vm.Items.Sum(x => x.LineTotal);

            return View(vm);
        }



    }
}
