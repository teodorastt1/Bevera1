using Bevera.Models.Catalog;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string ProductName { get; set; } = "";

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Range(1, 999)]
        public int Quantity { get; set; }

        [Range(0, 999999)]
        public decimal UnitPrice { get; set; }

        [Range(0, 9999999)]
        public decimal LineTotal { get; set; }
    }
}
