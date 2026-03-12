using Bevera.Data;
using Bevera.Helpers;
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
            var now = DateTime.UtcNow;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            // Основни
            var categoriesCount = await _db.Categories.CountAsync();
            var productsCount = await _db.Products.CountAsync();
            var ordersCount = await _db.Orders.CountAsync();
            var usersCount = await _db.Users.CountAsync();

            // Поръчки
            var pendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Submitted);
            var preparingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Preparing);
            var deliveredOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Delivered);
            var receivedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Received);
            var paidOrders = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Paid);

            var newOrdersToday = await _db.Orders.CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow);

            // Склад
            var lowStockProducts = await _db.Products
                .CountAsync(p => p.StockQty > 0 && p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 5));

            var outOfStockProducts = await _db.Products
                .CountAsync(p => p.StockQty <= 0);

            var activeProducts = await _db.Products.CountAsync(p => p.IsActive);
            var inactiveProducts = await _db.Products.CountAsync(p => !p.IsActive);

            // Промоции
            var activePromotions = await _db.Products.CountAsync(p =>
                p.IsActive &&
                p.DiscountPercent.HasValue &&
                p.DiscountPercent.Value > 0 &&
                (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= now));

            var endingSoonPromotions = await _db.Products.CountAsync(p =>
                p.IsActive &&
                p.DiscountPercent.HasValue &&
                p.DiscountPercent.Value > 0 &&
                p.DiscountEndsAt.HasValue &&
                p.DiscountEndsAt.Value >= now &&
                p.DiscountEndsAt.Value <= now.AddDays(2));

            // Финанси
            var revenue = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Income)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var expenses = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Expense)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var companyBalance = await _db.CompanyBalances
                .Select(x => (decimal?)x.Balance)
                .FirstOrDefaultAsync() ?? 0m;

            var grossProfit = revenue - expenses;

            // Supply
            var distributorsCount = await _db.Distributors.CountAsync(d => d.IsActive);
            var draftPurchaseOrders = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Draft);
            var submittedPurchaseOrders = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Submitted);
            var receivedPurchaseOrders = await _db.PurchaseOrders.CountAsync(x => x.Status == PurchaseOrderStates.Received);

            var model = new AdminDashboardViewModel
            {
                CategoriesCount = categoriesCount,
                ProductsCount = productsCount,
                OrdersCount = ordersCount,
                UsersCount = usersCount,

                PendingOrders = pendingOrders,
                PreparingOrders = preparingOrders,
                DeliveredOrders = deliveredOrders,
                ReceivedOrders = receivedOrders,
                PaidOrders = paidOrders,

                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                ActiveProducts = activeProducts,
                InactiveProducts = inactiveProducts,

                ActivePromotions = activePromotions,
                EndingSoonPromotions = endingSoonPromotions,

                Revenue = revenue,
                Expenses = expenses,
                GrossProfit = grossProfit,
                CompanyBalance = companyBalance,

                DistributorsCount = distributorsCount,
                DraftPurchaseOrders = draftPurchaseOrders,
                SubmittedPurchaseOrders = submittedPurchaseOrders,
                ReceivedPurchaseOrders = receivedPurchaseOrders,

                NewOrdersToday = newOrdersToday,
                LowStockAlerts = lowStockProducts
            };

            return View(model);
        }
    }
}