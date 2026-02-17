namespace Bevera.Models.ViewModel
{
    public class WorkerDashboardVm
    {
        public int NewOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int LowStockProducts { get; set; }

        // Плащания (симулация)
        public int AwaitingPayment { get; set; }
        public int Paid { get; set; }
    }
}
