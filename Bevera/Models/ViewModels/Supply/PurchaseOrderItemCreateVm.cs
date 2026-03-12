using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels.Supply
{
    public class PurchaseOrderItemCreateVm
    {
        public int PurchaseOrderId { get; set; }

        [Required(ErrorMessage = "Избери продукт.")]
        [Display(Name = "Продукт")]
        public int ProductId { get; set; }

        [Range(1, 999999, ErrorMessage = "Количеството трябва да е поне 1.")]
        [Display(Name = "Количество")]
        public int Quantity { get; set; }

        [Range(0, 999999, ErrorMessage = "Невалидна цена.")]
        [Display(Name = "Доставна цена")]
        public decimal CostPrice { get; set; }

        public List<SelectListItem> Products { get; set; } = new();
    }
}