using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bevera.Models.ViewModel
{
    public class AdminProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(140, ErrorMessage = "Името не може да е по-дълго от 140 символа.")]
        [Display(Name = "Име")]
        public string Name { get; set; } = "";

        [StringLength(80, ErrorMessage = "SKU не може да е по-дълго от 80 символа.")]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }

        [StringLength(500, ErrorMessage = "Описанието не може да е по-дълго от 500 символа.")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Range(0, 999999, ErrorMessage = "Продажната цена трябва да е между 0 и 999999.")]
        [Display(Name = "Продажна цена")]
        public decimal Price { get; set; }

        [Range(0, 999999, ErrorMessage = "Доставната цена трябва да е между 0 и 999999.")]
        [Display(Name = "Доставна цена")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Избери категория.")]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        [Display(Name = "Марка")]
        public int? BrandId { get; set; }

        [Range(0, 999999, ErrorMessage = "Наличността трябва да е между 0 и 999999.")]
        [Display(Name = "Наличност")]
        public int StockQty { get; set; }

        [Range(0, 999999, ErrorMessage = "Прагът за ниска наличност трябва да е между 0 и 999999.")]
        [Display(Name = "Праг за ниска наличност")]
        public int LowStockThreshold { get; set; } = 5;

        [Range(0, 100000, ErrorMessage = "Милилитрите трябва да са между 0 и 100000.")]
        [Display(Name = "Милилитри")]
        public int? Ml { get; set; }

        [StringLength(40, ErrorMessage = "Типът опаковка не може да е по-дълъг от 40 символа.")]
        [Display(Name = "Тип опаковка")]
        public string? PackageType { get; set; }

        [Range(0, 90, ErrorMessage = "Отстъпката трябва да е между 0 и 90%.")]
        [Display(Name = "Отстъпка (%)")]
        public decimal? DiscountPercent { get; set; }

        [Display(Name = "Край на отстъпката")]
        [DataType(DataType.DateTime)]
        public DateTime? DiscountEndsAt { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        // dropdown-и
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Brands { get; set; } = new();

        // upload
        [Display(Name = "Снимка")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        // удобни readonly helper-и за view
        public bool HasPromotion =>
            DiscountPercent.HasValue && DiscountPercent.Value > 0 &&
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
    }
}