using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // 1) Basic counts
            var categoriesCount = await _db.Categories.CountAsync();
            var productsCount = await _db.Products.CountAsync();

            // 2) Orders
            var ordersCount = await _db.Orders.CountAsync();
            var pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Submitted);
            var deliveredOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Delivered);

            // 3) Revenue
            var revenue = await _db.Orders
                .Where(o => o.Status != OrderStates.Cancelled)
                .SumAsync(o => (decimal?)o.Total) ?? 0m;

            // 4) Stock indicators
            var lowStockProducts = await _db.Products
                .CountAsync(p => p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 10));

            // 5) Users count
            var usersCount = await _db.Users.CountAsync();

            // 6) Promotions
            var now = DateTime.UtcNow;

            var activePromotions = await _db.Products.CountAsync(p =>
                p.DiscountPercent.HasValue &&
                p.DiscountPercent.Value > 0 &&
                (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now));

            var endingSoonPromotions = await _db.Products.CountAsync(p =>
                p.DiscountPercent.HasValue &&
                p.DiscountPercent.Value > 0 &&
                p.DiscountEndsAt.HasValue &&
                p.DiscountEndsAt.Value >= now &&
                p.DiscountEndsAt.Value <= now.AddDays(2));

            var model = new AdminDashboardViewModel
            {
                CategoriesCount = categoriesCount,
                ProductsCount = productsCount,
                OrdersCount = ordersCount,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                UsersCount = usersCount,
                Revenue = revenue,
                LowStockProducts = lowStockProducts,
                ActivePromotions = activePromotions,
                EndingSoonPromotions = endingSoonPromotions
            };

            return View(model);
        }
    }
}