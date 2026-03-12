using Bevera.Models.Catalog;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Supply
{
    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required, StringLength(140)]
        public string ProductName { get; set; } = "";

        [Range(1, 999999)]
        public int Quantity { get; set; }

        [Range(0, 999999)]
        public decimal CostPrice { get; set; }

        [Range(0, 99999999)]
        public decimal LineTotal { get; set; }
    }
}