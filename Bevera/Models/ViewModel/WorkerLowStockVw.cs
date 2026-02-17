namespace Bevera.Models.ViewModel
{
    public class WorkerLowStockVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
    }
}
