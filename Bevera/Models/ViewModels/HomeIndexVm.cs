using Bevera.Models.Catalog;

namespace Bevera.Models.ViewModels
{
    public class HomeIndexVm
    {
        public List<HomeProductCardVm> BestSellers { get; set; } = new();
        public List<HomeProductCardVm> Newest { get; set; } = new();
    }

    public class HomeProductCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public bool IsHit { get; set; }

        public static HomeProductCardVm From(Product p, bool isHit)
        {
            var img = p.Images?
                .OrderByDescending(i => i.IsMain)
                .Select(i => i.ImagePath)
                .FirstOrDefault();

            return new HomeProductCardVm
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImagePath = img,
                IsHit = isHit
            };
        }
    }
}
