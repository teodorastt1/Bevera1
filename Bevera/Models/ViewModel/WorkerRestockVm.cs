namespace Bevera.Models.ViewModel
{
    public class WorkerRestockVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public int CurrentQuantity { get; set; }
        public int MinQuantity { get; set; }

        public int AddQuantity { get; set; }
    }
}
