using Bevera.Models.Catalog;

namespace Bevera.Models.ViewModels
{
    public class HomeIndexVm
    {
        public List<HomeProductCardVm> BestSellers { get; set; } = new();
        public List<HomeProductCardVm> Newest { get; set; } = new();
        public List<HomeProductCardVm> Promotions { get; set; } = new();
    }

    public class HomeProductCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public decimal EffectivePrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public DateTime? DiscountEndsAt { get; set; }
        public bool IsDiscounted { get; set; }
        public string? ImagePath { get; set; }
        public bool IsHit { get; set; }

        public static HomeProductCardVm From(Product p, bool isHit)
        {
            var img = p.Images?
                .OrderByDescending(i => i.IsMain)
                .Select(i => i.ImagePath)
                .FirstOrDefault();

            var hasDiscount =
                p.DiscountPercent.HasValue &&
                p.DiscountPercent.Value > 0 &&
                (!p.DiscountEndsAt.HasValue || p.DiscountEndsAt.Value >= DateTime.UtcNow);

            return new HomeProductCardVm
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                EffectivePrice = p.EffectivePrice,
                DiscountPercent = p.DiscountPercent,
                DiscountEndsAt = p.DiscountEndsAt,
                IsDiscounted = hasDiscount,
                ImagePath = img,
                IsHit = isHit
            };
        }
    }
}