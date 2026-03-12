using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Supply
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        public int DistributorId { get; set; }
        public Distributor Distributor { get; set; } = null!;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Draft";
        // Draft, Submitted, Received, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = "";
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public string? ReceivedByUserId { get; set; }
        public ApplicationUser? ReceivedByUser { get; set; }

        [Range(0, 99999999)]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}