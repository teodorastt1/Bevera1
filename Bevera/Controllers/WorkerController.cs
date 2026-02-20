using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Inventory;
using Bevera.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                LowStockProducts = await _db.Products.CountAsync(p =>
                p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10)),
                AwaitingPayment = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Unpaid),
                Paid = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Paid)
            };

            return View(vm);
        }

        public async Task<IActionResult> LowStock()
        {
            var items = await _db.Products
                .AsNoTracking()
                .Where(p => p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10))
                .OrderBy(p => p.Name)
                .Select(p => new WorkerLowStockVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Quantity = p.StockQty,
                    MinQuantity = (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10)
                })
                .ToListAsync();

            return View(items);
        }


        [Authorize(Roles = "Worker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(WorkerRestockVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            if (vm.AddQuantity <= 0)
            {
                ModelState.AddModelError(nameof(vm.AddQuantity), "Въведи положително количество.");
                return View(vm);
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == vm.ProductId);
            if (product == null)
                return NotFound();

            product.StockQty += vm.AddQuantity;
            product.Quantity = product.StockQty;

            _db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = product.Id,
                QuantityDelta = vm.AddQuantity,
                Type = "Restock",
                Note = $"Worker restock +{vm.AddQuantity}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Успешно заредено: {product.Name} (+{vm.AddQuantity})";
            return RedirectToAction("LowStock");
        }



        [Authorize(Roles = "Worker")]
        [HttpGet]
        public async Task<IActionResult> Restock(int id)
        {
            var product = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var vm = new WorkerRestockVm
            {
                ProductId = product.Id,
                Name = product.Name,
                CurrentQuantity = product.StockQty,
                MinQuantity = (product.LowStockThreshold > 0 ? product.LowStockThreshold : 10)
            };

            return View(vm);
        }



    }
}
