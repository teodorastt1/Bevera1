using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels.Supply
{
    public class PurchaseOrderCreateVm
    {
        [Required(ErrorMessage = "Избери дистрибутор.")]
        [Display(Name = "Дистрибутор")]
        public int DistributorId { get; set; }

        [Display(Name = "Бележки")]
        [StringLength(500)]
        public string? Notes { get; set; }

        public List<SelectListItem> Distributors { get; set; } = new();
    }
}