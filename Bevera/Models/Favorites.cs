using Bevera.Models.Catalog;
using System;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
