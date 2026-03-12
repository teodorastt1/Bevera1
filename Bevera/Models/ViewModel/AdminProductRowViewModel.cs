namespace Bevera.Models.ViewModel
{
    public class AdminProductRowViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
        public string? SKU { get; set; }

        public string CategoryName { get; set; } = "";
        public string? BrandName { get; set; }

        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }

        public int StockQty { get; set; }
        public int LowStockThreshold { get; set; }

        public int? Ml { get; set; }
        public string? PackageType { get; set; }

        public decimal? DiscountPercent { get; set; }
        public DateTime? DiscountEndsAt { get; set; }

        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }

        public bool HasPromotion =>
            DiscountPercent.HasValue &&
            DiscountPercent.Value > 0 &&
            (!DiscountEndsAt.HasValue || DiscountEndsAt.Value >= DateTime.UtcNow);

        public decimal EffectivePrice
        {
            get
            {
                if (HasPromotion)
                {
                    var discounted = Price * (1 - (DiscountPercent!.Value / 100m));
                    return discounted < 0 ? 0 : decimal.Round(discounted, 2);
                }

                return Price;
            }
        }

        public decimal UnitProfit => EffectivePrice - CostPrice;

        public bool IsOutOfStock => StockQty <= 0;

        public bool IsLowStock =>
            StockQty > 0 &&
            StockQty <= (LowStockThreshold > 0 ? LowStockThreshold : 5);

        public string StockStatus
        {
            get
            {
                if (!IsActive) return "Неактивен";
                if (IsOutOfStock) return "Изчерпан";
                if (IsLowStock) return "Ниска наличност";
                return "Наличен";
            }
        }
    }
}