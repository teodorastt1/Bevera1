namespace Bevera.Models.ViewModel
{
    public class AdminDashboardViewModel
    {
        public int CategoriesCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }

        public decimal Revenue { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int LowStockProducts { get; set; }

        // ✅ ново
        public int UsersCount { get; set; }
    }
}
