using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels
{
    public class AdminCategoryFormViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public string? ExistingImagePath { get; set; }

        public IFormFile? ImageFile { get; set; }
        public int? ParentCategoryId { get; set; } // null = основна
        public List<SelectListItem> ParentOptions { get; set; } = new();

    }
}
