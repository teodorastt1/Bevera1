using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bevera.Models.Catalog
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Name { get; set; } = ""; // "Coca-Cola"

        [StringLength(80)]
        public string? SKU { get; set; } // по желание

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        // ✅ Promotions / discounts (requires migration)
        // Ако DiscountPercent има стойност и (ако има) DiscountEndsAt не е изтекло,
        // продуктът се продава на намалена цена.
        [Range(0, 90)]
        public decimal? DiscountPercent { get; set; }

        public DateTime? DiscountEndsAt { get; set; }

        // drink-specific
        [Range(0, 10)]
        public decimal VolumeLiters { get; set; } // 0.5, 1.5...
        [Range(0, 999999)]
        public int StockQty { get; set; }

        // под това число => “LOW STOCK”
        [Range(0, 999999)]
        public int LowStockThreshold { get; set; } = 5;

        [Range(0, 100)]
        public decimal AlcoholPercent { get; set; } // 0 за безалкохолно

        [StringLength(40)]
        public string? PackageType { get; set; } // "Bottle", "Can"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // relations
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public int Quantity { get; set; }
        public int MinQuantity { get; set; }

        // Not mapped helper
        [NotMapped]
        public decimal EffectivePrice
        {
            get
            {
                if (DiscountPercent.HasValue && DiscountPercent.Value > 0)
                {
                    if (!DiscountEndsAt.HasValue || DiscountEndsAt.Value >= DateTime.UtcNow)
                    {
                        var pct = DiscountPercent.Value / 100m;
                        var discounted = Price * (1m - pct);
                        return discounted < 0 ? 0 : decimal.Round(discounted, 2);
                    }
                }
                return Price;
            }
        }

    }
}
