namespace Bevera.Models.ViewModel
{
    public class WorkerOrderListVm
    {
        public int OrderId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Status { get; set; } = "";
    }
}
