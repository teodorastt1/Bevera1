using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        [Display(Name = "Delivery Address")]
        public string DeliveryAddress { get; set; } = "";

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "CashOnDelivery"; // Card / CashOnDelivery

        // Card fields (ако избереш Card)
        public string? CardOwner { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpMonth { get; set; }
        public string? ExpYear { get; set; }
        public string? CVV { get; set; }
    }
}
