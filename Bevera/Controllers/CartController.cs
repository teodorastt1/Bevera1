using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bevera.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CartKey = "CART";

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        private Dictionary<int, int> GetCart()
            => HttpContext.Session.GetObject<Dictionary<int, int>>(CartKey) ?? new Dictionary<int, int>();

        private void SaveCart(Dictionary<int, int> cart)
            => HttpContext.Session.SetObject(CartKey, cart);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            var ids = cart.Keys.ToList();

            var products = await _db.Set<Product>()
                .Where(p => ids.Contains(p.Id))
                .Include(p => p.Images)
                .ToListAsync();

            var items = products.Select(p =>
            {
                var img = p.Images?.FirstOrDefault(i => i.IsMain)?.ImagePath
                          ?? p.Images?.FirstOrDefault()?.ImagePath
                          ?? "/images/image-1.jpg";

                return new CartItemVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ImagePath = img,
                    UnitPrice = p.EffectivePrice,
                    Quantity = cart[p.Id]
                };
            }).OrderBy(i => i.Name).ToList();

            ViewBag.GrandTotal = items.Sum(i => i.Total);
            return View(items); // Views/Cart/Index.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int qty = 1, string? returnUrl = null)
        {
            if (qty < 1) qty = 1;

            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
            {
                TempData["FlashMessage"] = "Продуктът не е намерен.";
                TempData["FlashType"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            var available = product.Quantity > 0 ? product.Quantity : product.StockQty;
            if (available <= 0)
            {
                TempData["FlashMessage"] = $"{product.Name} в момента не е наличен.";
                TempData["FlashType"] = "danger";
                return !string.IsNullOrWhiteSpace(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
            }

            var cart = GetCart();
            var current = cart.ContainsKey(productId) ? cart[productId] : 0;
            var desired = current + qty;

            if (desired > available)
            {
                desired = available;
                TempData["FlashMessage"] = $"Няма достатъчно наличност за {product.Name}. Налично: {available}.";
                TempData["FlashType"] = "danger";
            }

            cart[productId] = desired;

            SaveCart(cart);

            // used by navbar badge animation
            TempData["CartPulse"] = 1;

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, int qty)
        {
            var cart = GetCart();

            if (qty <= 0)
                cart.Remove(productId);
            else
            {
                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
                if (product == null)
                {
                    cart.Remove(productId);
                    TempData["FlashMessage"] = "Продуктът вече не е наличен.";
                    TempData["FlashType"] = "danger";
                }
                else
                {
                    var available = product.Quantity > 0 ? product.Quantity : product.StockQty;
                    if (available <= 0)
                    {
                        cart.Remove(productId);
                        TempData["FlashMessage"] = $"{product.Name} в момента не е наличен.";
                        TempData["FlashType"] = "danger";
                    }
                    else
                    {
                        if (qty > available)
                        {
                            qty = available;
                            TempData["FlashMessage"] = $"Няма достатъчно наличност за {product.Name}. Налично: {available}.";
                            TempData["FlashType"] = "danger";
                        }

                        cart[productId] = qty;
                    }
                }
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.Remove(productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            // Cart is session-based in this project.
            var cart = GetCart();
            var count = cart.Values.Sum();
            return Json(new { count });
        }

        // =========================
        // CHECKOUT (payment simulation)
        // =========================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            var ids = cart.Keys.ToList();
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .Include(p => p.Images)
                .ToListAsync();

            var items = products.Select(p => new CheckoutItemVm
            {
                ProductId = p.Id,
                Name = p.Name,
                UnitPrice = p.EffectivePrice,
                Quantity = cart[p.Id]
            }).ToList();

            var vm = new CheckoutVm
            {
                Items = items,
                Total = items.Sum(i => i.Total)
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutVm vm)
        {
            var cart = GetCart();
            if (cart.Count == 0) return RedirectToAction(nameof(Index));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            // reload products from DB
            var ids = cart.Keys.ToList();
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            // validate stock
            foreach (var p in products)
            {
                var qty = cart[p.Id];
                var available = p.Quantity > 0 ? p.Quantity : p.StockQty;
                if (available < qty)
                {
                    ModelState.AddModelError("", $"Няма достатъчно наличност за: {p.Name}. Налично: {available}");
                }
            }

            if (!ModelState.IsValid)
            {
                vm.Items = products.Select(p => new CheckoutItemVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    UnitPrice = p.EffectivePrice,
                    Quantity = cart[p.Id]
                }).ToList();
                vm.Total = vm.Items.Sum(i => i.Total);
                return View(vm);
            }

            var isCard = vm.PaymentMethod == "card";

            // Extra safety: if cash, ignore any card fields
            if (!isCard)
            {
                vm.CardHolder = null;
                vm.CardNumber = null;
                vm.ExpMonth = null;
                vm.ExpYear = null;
                vm.Cvc = null;
            }

            var order = new Order
            {
                ClientId = userId,
                CreatedAt = DateTime.UtcNow,
                ChangedAt = DateTime.UtcNow,
                Status = OrderStates.Submitted,
                PaymentStatus = isCard ? PaymentStates.Paid : PaymentStates.Unpaid,
                PaidOn = isCard ? DateTime.UtcNow : null,
                FullName = vm.FullName ?? "",
                Email = vm.Email ?? "",
                Phone = vm.Phone?.Trim(),
                Address = $"{(vm.City ?? "").Trim()}, {(vm.Address ?? "").Trim()}".Trim().Trim(','),
                Total = products.Sum(p => p.EffectivePrice * cart[p.Id])
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var p in products)
            {
                var qty = cart[p.Id];

                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Quantity = qty,
                    UnitPrice = p.EffectivePrice,
                    LineTotal = p.EffectivePrice * qty
                });

                if (p.Quantity > 0) p.Quantity -= qty;
                if (p.StockQty > 0) p.StockQty -= qty;

                _db.InventoryMovements.Add(new Bevera.Models.Inventory.InventoryMovement
                {
                    ProductId = p.Id,
                    QuantityDelta = -qty,
                    Type = "OUT",
                    Note = $"Order #{order.Id} checkout",
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    OrderId = order.Id
                });
            }

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStates.Submitted,
                Note = isCard ? "Плащане: карта (симулация, прието)." : "Плащане: наложен платеж (очаква се).",
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId
            });

            await _db.SaveChangesAsync();

            SaveCart(new Dictionary<int, int>());

            TempData["OrderSuccess"] = "Поръчката е направена успешно!";
            return RedirectToAction("Profile", "Client");
        }
    }
}
