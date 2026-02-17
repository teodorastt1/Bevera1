using Bevera.Models.Catalog;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        // snapshot цена в момента на добавяне
        [Range(0, 999999)]
        public decimal UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
