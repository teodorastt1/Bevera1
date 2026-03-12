namespace Bevera.Models.ViewModel
{
    public class AdminDashboardViewModel
    {
        // Основни
        public int CategoriesCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public int UsersCount { get; set; }

        // Поръчки
        public int PendingOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int ReceivedOrders { get; set; }
        public int PaidOrders { get; set; }

        // Склад
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }

        // Промоции
        public int ActivePromotions { get; set; }
        public int EndingSoonPromotions { get; set; }

        // Финанси
        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal CompanyBalance { get; set; }

        // Supply
        public int DistributorsCount { get; set; }
        public int DraftPurchaseOrders { get; set; }
        public int SubmittedPurchaseOrders { get; set; }
        public int ReceivedPurchaseOrders { get; set; }

        // Полезни за таблото
        public int NewOrdersToday { get; set; }
        public int LowStockAlerts { get; set; }
    }
}