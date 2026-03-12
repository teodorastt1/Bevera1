using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Finance
{
    public class FinanceTransaction
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string Type { get; set; } = "";
        // Income, Expense

        [Required, StringLength(50)]
        public string Source { get; set; } = "";
        // Order, PurchaseOrder, Manual

        [Range(0, 999999999)]
        public decimal Amount { get; set; }

        [Required, StringLength(250)]
        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? OrderId { get; set; }
        public int? PurchaseOrderId { get; set; }

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
    }
}