using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Finance
{
    public class CompanyBalance
    {
        public int Id { get; set; }

        [Range(0, 999999999)]
        public decimal Balance { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}