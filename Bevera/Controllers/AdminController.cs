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

        // Order/Payment are stored as string in the database.
        // Keep them consistent via shared constants.

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

            // 3) Revenue (само paid + delivered)
            var revenue = await _db.Orders
                .Where(o => o.PaymentStatus == PaymentStates.Paid && o.Status == OrderStates.Delivered)
                .SumAsync(o => (decimal?)o.Total) ?? 0m;

            // 4) Stock indicators
            var lowStockProducts = await _db.Products
                .CountAsync(p => p.Quantity < p.MinQuantity);

            // 5) Users count (всички)
            var usersCount = await _db.Users.CountAsync();

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

            };

            return View(model);
        }
    }
}
