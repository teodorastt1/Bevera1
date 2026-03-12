namespace Bevera.Models.ViewModels.Supply
{
    public class SupplyDashboardVm
    {
        public decimal CompanyBalance { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal GrossProfit { get; set; }

        public int DistributorsCount { get; set; }
        public int DraftOrdersCount { get; set; }
        public int SubmittedOrdersCount { get; set; }
        public int ReceivedOrdersCount { get; set; }

        public List<PurchaseOrderShortVm> LatestOrders { get; set; } = new();
    }

    public class PurchaseOrderShortVm
    {
        public int Id { get; set; }
        public string DistributorName { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}