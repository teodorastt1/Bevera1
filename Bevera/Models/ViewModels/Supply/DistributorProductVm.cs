using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels.Supply
{
    public class DistributorProductVm
    {
        public int Id { get; set; }

        public int DistributorId { get; set; }

        [Display(Name = "Продукт")]
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        [Range(0, 999999)]
        [Display(Name = "Доставна цена")]
        public decimal CostPrice { get; set; }

        [Display(Name = "Наличен при дистрибутора")]
        public bool IsAvailable { get; set; } = true;
    }
}