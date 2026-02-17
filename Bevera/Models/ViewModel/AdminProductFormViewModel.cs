using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bevera.Models.ViewModels
{
    public class AdminProductFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Range(0, 999999)]
        public int StockQty { get; set; }

        [Range(0, 999999)]
        public int LowStockThreshold { get; set; } = 5;

        public bool IsActive { get; set; } = true;

        // за dropdown
        public List<SelectListItem> Categories { get; set; } = new();

        // upload
        public IFormFile? ImageFile { get; set; }
        public string? ExistingImagePath { get; set; }
    }
}
