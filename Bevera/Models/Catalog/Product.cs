using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bevera.Models.Catalog
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Name { get; set; } = "";

        [StringLength(80)]
        public string? SKU { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        // НОВО: доставна цена (колко фирмата го купува)
        [Range(0, 999999)]
        public decimal CostPrice { get; set; }

        // НОВО: мл за филтър и показване
        [Range(0, 100000)]
        public int? Ml { get; set; }

        [Range(0, 90)]
        public decimal? DiscountPercent { get; set; }

        public DateTime? DiscountEndsAt { get; set; }

        [Range(0, 10)]
        public decimal VolumeLiters { get; set; }

        [Range(0, 999999)]
        public int StockQty { get; set; }

        [Range(0, 999999)]
        public int LowStockThreshold { get; set; } = 5;

        [Range(0, 100)]
        public decimal AlcoholPercent { get; set; }

        [StringLength(40)]
        public string? PackageType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public int Quantity { get; set; }
        public int MinQuantity { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

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

        [NotMapped]
        public decimal UnitProfit
        {
            get
            {
                var salePrice = EffectivePrice;
                return salePrice - CostPrice;
            }
        }
    }
}