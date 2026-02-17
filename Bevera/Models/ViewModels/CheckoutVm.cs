using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels
{
    public class CheckoutVm
    {
        public List<CheckoutItemVm> Items { get; set; } = new();

        public decimal Total { get; set; }

        [Required(ErrorMessage = "Въведи име.")]
        [StringLength(120)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Въведи имейл.")]
        [EmailAddress]
        [StringLength(120)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        // "card" or "cash" (simulation)
        [Required]
        public string PaymentMethod { get; set; } = "card";
    }

    public class CheckoutItemVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }
}
