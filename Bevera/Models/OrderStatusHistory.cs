using System;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [Required, StringLength(20)]
        public string Status { get; set; } = "";

        [StringLength(250)]
        public string? Note { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // кой го е сменил (Admin/Worker)
        [Required]
        public string ChangedByUserId { get; set; } = "";
        public ApplicationUser ChangedByUser { get; set; } = null!;
    }
}
