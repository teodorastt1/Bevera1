using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModel
{
    public class AdminPromotionFormViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public decimal Price { get; set; }

        [Range(0, 90, ErrorMessage = "Намалението трябва да е между 0 и 90%.")]
        public decimal? DiscountPercent { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DiscountEndsAt { get; set; }
    }
}