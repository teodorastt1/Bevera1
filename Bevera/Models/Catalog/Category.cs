using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Catalog
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ NEW: Parent / Children
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
