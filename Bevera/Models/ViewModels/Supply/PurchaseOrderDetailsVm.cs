namespace Bevera.Models.ViewModels.Supply
{
    public class PurchaseOrderDetailsVm
    {
        public int Id { get; set; }
        public string DistributorName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }

        public List<PurchaseOrderItemRowVm> Items { get; set; } = new();
    }

    public class PurchaseOrderItemRowVm
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}