namespace Bevera.Models.ViewModels
{
    public class AdminProductRowViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }

        public bool IsLowStock => StockQty <= LowStockThreshold;
    }
}
