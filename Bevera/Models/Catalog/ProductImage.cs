using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Catalog
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Required, StringLength(300)]
        public string ImagePath { get; set; } = ""; // /uploads/products/xxx.jpg

        public bool IsMain { get; set; } = false;
    }
}
