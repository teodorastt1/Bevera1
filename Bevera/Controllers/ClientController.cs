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
        public async Task<IActionResult> Category(
            int id,
            string? q,
            decimal? minPrice,
            decimal? maxPrice,
            bool onlyAvailable = false,
            bool onlyPromo = false,
            int? brandId = null,
            int? minMl = null,
            int? maxMl = null,
            string? packageType = null,
            string? sort = null)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null) return NotFound();

            // Ако е parent категория -> показваме подкатегориите
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
                .Include(p => p.Brand)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                productsQuery = productsQuery.Where(p => p.Name.Contains(q));
            }

            // Price range (по EffectivePrice, не по Price)
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.EffectivePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.EffectivePrice <= maxPrice.Value);
            }

            // Only available
            if (onlyAvailable)
            {
                productsQuery = productsQuery.Where(p => p.StockQty > 0);
            }

            // Only promotions
            if (onlyPromo)
            {
                productsQuery = productsQuery.Where(p =>
                    p.DiscountPercent.HasValue &&
                    p.DiscountPercent.Value > 0 &&
                    (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= DateTime.UtcNow));
            }

            // Brand
            if (brandId.HasValue && brandId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
            }

            // Min ml
            if (minMl.HasValue)
            {
                var minLiters = minMl.Value / 1000m;
                productsQuery = productsQuery.Where(p => p.VolumeLiters >= minLiters);
            }

            // Max ml
            if (maxMl.HasValue)
            {
                var maxLiters = maxMl.Value / 1000m;
                productsQuery = productsQuery.Where(p => p.VolumeLiters <= maxLiters);
            }

            // Package type
            if (!string.IsNullOrWhiteSpace(packageType))
            {
                productsQuery = productsQuery.Where(p => p.PackageType != null && p.PackageType == packageType);
            }

            // Sorting
            productsQuery = sort switch
            {
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                "price_asc" => productsQuery.OrderBy(p => p.EffectivePrice).ThenBy(p => p.Name),
                "price_desc" => productsQuery.OrderByDescending(p => p.EffectivePrice).ThenBy(p => p.Name),
                "promo_desc" => productsQuery
                    .OrderByDescending(p => p.DiscountPercent.HasValue ? p.DiscountPercent.Value : 0)
                    .ThenBy(p => p.Name),
                "ml_asc" => productsQuery.OrderBy(p => p.VolumeLiters).ThenBy(p => p.Name),
                "ml_desc" => productsQuery.OrderByDescending(p => p.VolumeLiters).ThenBy(p => p.Name),
                _ => productsQuery.OrderBy(p => p.Name)
            };

            // Dropdown/filter data
            var brands = await _db.Brands
                .Where(b => b.IsActive && b.Products.Any(p => p.CategoryId == id && p.IsActive))
                .OrderBy(b => b.Name)
                .ToListAsync();

            var packageTypes = await _db.Products
                .Where(p => p.CategoryId == id && p.IsActive && p.PackageType != null && p.PackageType != "")
                .Select(p => p.PackageType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var products = await productsQuery.ToListAsync();

            ViewBag.CategoryName = category.Name;
            ViewBag.Q = q;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.OnlyAvailable = onlyAvailable;
            ViewBag.OnlyPromo = onlyPromo;
            ViewBag.BrandId = brandId;
            ViewBag.MinMl = minMl;
            ViewBag.MaxMl = maxMl;
            ViewBag.PackageType = packageType;
            ViewBag.Sort = sort ?? "";
            ViewBag.Brands = brands;
            ViewBag.PackageTypes = packageTypes;

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
                .Include(x => x.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (p == null) return NotFound();

            bool canReview = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(userId) && User.IsInRole("Client"))
                {
                    canReview = await _db.OrderItems
                        .Include(oi => oi.Order)
                        .AnyAsync(oi =>
                            oi.ProductId == id &&
                            oi.Order.ClientId == userId &&
                            oi.Order.Status == OrderStates.Received);

                    var alreadyReviewed = await _db.Reviews
                        .AnyAsync(r => r.ProductId == id && r.UserId == userId);

                    if (alreadyReviewed)
                        canReview = false;
                }
            }

            ViewBag.CanReview = canReview;

            return View(p);
        }

        // Search from navbar
        [HttpGet]
        public async Task<IActionResult> Search(string? q)
        {
            q = q?.Trim();

            ViewBag.Q = q;

            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return View(new List<Bevera.Models.Catalog.Product>());
            }

            var products = await _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)))
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Promotions(
            string? q,
            decimal? minPrice,
            decimal? maxPrice,
            bool onlyAvailable = false,
            int? brandId = null,
            int? categoryId = null,
            int? minMl = null,
            int? maxMl = null,
            string? packageType = null,
            string? sort = null)
        {
            var now = DateTime.UtcNow;

            var productsQuery = _db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive
                    && p.DiscountPercent.HasValue
                    && p.DiscountPercent.Value > 0
                    && (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                productsQuery = productsQuery.Where(p => p.Name.Contains(q));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.EffectivePrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.EffectivePrice <= maxPrice.Value);
            }

            if (onlyAvailable)
            {
                productsQuery = productsQuery.Where(p => p.StockQty > 0);
            }

            if (brandId.HasValue && brandId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minMl.HasValue)
            {
                var minLiters = minMl.Value / 1000m;
                productsQuery = productsQuery.Where(p => p.VolumeLiters >= minLiters);
            }

            if (maxMl.HasValue)
            {
                var maxLiters = maxMl.Value / 1000m;
                productsQuery = productsQuery.Where(p => p.VolumeLiters <= maxLiters);
            }

            if (!string.IsNullOrWhiteSpace(packageType))
            {
                productsQuery = productsQuery.Where(p => p.PackageType != null && p.PackageType == packageType);
            }

            productsQuery = sort switch
            {
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                "price_asc" => productsQuery.OrderBy(p => p.EffectivePrice).ThenBy(p => p.Name),
                "price_desc" => productsQuery.OrderByDescending(p => p.EffectivePrice).ThenBy(p => p.Name),
                "promo_desc" => productsQuery.OrderByDescending(p => p.DiscountPercent ?? 0).ThenBy(p => p.Name),
                "ml_asc" => productsQuery.OrderBy(p => p.VolumeLiters).ThenBy(p => p.Name),
                "ml_desc" => productsQuery.OrderByDescending(p => p.VolumeLiters).ThenBy(p => p.Name),
                "end_asc" => productsQuery.OrderBy(p => p.DiscountEndsAt).ThenBy(p => p.Name),
                _ => productsQuery.OrderBy(p => p.Name)
            };

            var brands = await _db.Brands
                .Where(b => b.IsActive && b.Products.Any(p =>
                    p.IsActive &&
                    p.DiscountPercent.HasValue &&
                    p.DiscountPercent.Value > 0 &&
                    (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now)))
                .OrderBy(b => b.Name)
                .ToListAsync();

            var categories = await _db.Categories
                .Where(c => c.IsActive && c.Products.Any(p =>
                    p.IsActive &&
                    p.DiscountPercent.HasValue &&
                    p.DiscountPercent.Value > 0 &&
                    (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now)))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var packageTypes = await _db.Products
                .Where(p => p.IsActive
                    && p.DiscountPercent.HasValue
                    && p.DiscountPercent.Value > 0
                    && (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now)
                    && p.PackageType != null
                    && p.PackageType != "")
                .Select(p => p.PackageType!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var products = await productsQuery.ToListAsync();

            ViewBag.Q = q;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.OnlyAvailable = onlyAvailable;
            ViewBag.BrandId = brandId;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinMl = minMl;
            ViewBag.MaxMl = maxMl;
            ViewBag.PackageType = packageType;
            ViewBag.Sort = sort ?? "";
            ViewBag.Brands = brands;
            ViewBag.Categories = categories;
            ViewBag.PackageTypes = packageTypes;

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

            return View(vm);
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

            return View(favorites);
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

            TempData["ToastMessage"] = "Данните бяха запазени успешно.";
            TempData["ToastType"] = "success";

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

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);

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