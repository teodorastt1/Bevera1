using Bevera.Models.Catalog;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Supply
{
    public class DistributorProduct
    {
        public int Id { get; set; }

        public int DistributorId { get; set; }
        public Distributor Distributor { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Range(0, 999999)]
        public decimal CostPrice { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}