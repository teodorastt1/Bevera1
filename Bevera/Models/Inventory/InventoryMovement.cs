using Bevera.Models.Catalog;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Inventory
{
    public class InventoryMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // + за вход, - за изход
        public int QuantityDelta { get; set; }

        [Required, StringLength(30)]
        public string Type { get; set; } = "IN"; // IN, OUT, ADJUST

        [StringLength(200)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // кой го е направил (Admin/Worker)
        [Required]
        public string CreatedByUserId { get; set; } = "";
        public ApplicationUser CreatedByUser { get; set; } = null!;

        // ако е за поръчка
        public int? OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
