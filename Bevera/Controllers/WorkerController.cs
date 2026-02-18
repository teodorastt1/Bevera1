using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Inventory;
using Bevera.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorkerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
        {
            var vm = new WorkerDashboardVm
            {
                NewOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Submitted),
                PreparingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Preparing),
                ShippedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.ReadyForPickup),
                LowStockProducts = await _db.Products.CountAsync(p => ((p.Quantity > 0 ? p.Quantity : p.StockQty) > 0)
                                                                 && ((p.Quantity > 0 ? p.Quantity : p.StockQty) <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10))),

                AwaitingPayment = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Unpaid),
                Paid = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Paid)
            };

            return View(vm);
        }

        // =========================
        // 2) НИСКА НАЛИЧНОСТ / ЗАРЕЖДАНЕ
        // =========================
        public async Task<IActionResult> LowStock()
        {
            var items = await _db.Products
                .AsNoTracking()
                .Where(p => ((p.Quantity > 0 ? p.Quantity : p.StockQty) > 0)
                            && ((p.Quantity > 0 ? p.Quantity : p.StockQty) <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10)))
                .OrderBy(p => p.Name)
                .Select(p => new WorkerLowStockVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Quantity = (p.Quantity > 0 ? p.Quantity : p.StockQty),
                    MinQuantity = (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10)
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Restock(int id)
        {
            var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var vm = new WorkerRestockVm
            {
                ProductId = p.Id,
                Name = p.Name,
                CurrentQuantity = p.Quantity,
                MinQuantity = p.MinQuantity
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(WorkerRestockVm vm)
        {
            if (vm.AddQuantity <= 0)
                ModelState.AddModelError(nameof(vm.AddQuantity), "Въведи количество по-голямо от 0.");

            if (!ModelState.IsValid)
                return View(vm);

            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == vm.ProductId);
            if (p == null) return NotFound();

            p.Quantity += vm.AddQuantity;

            // Log inventory movement (IN)
            var userId = _userManager.GetUserId(User) ?? "";
            _db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = p.Id,
                QuantityDelta = vm.AddQuantity,
                Type = "IN",
                Note = $"Restock by worker (+{vm.AddQuantity})",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(LowStock));
        }
    }
}
