namespace Bevera.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber => $"#{OrderId}";
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "";

        public decimal Total { get; set; }
        public List<OrderDetailsItemViewModel> Items { get; set; } = new();
    }

    public class OrderDetailsItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;
    }
}
