using System;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class AppNotification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        public ApplicationUser User { get; set; } = default!;

        [Required]
        [StringLength(300)]
        public string Message { get; set; } = default!;

        [StringLength(100)]
        public string Type { get; set; } = "Info";
        // Examples: Order, LowStock, Promo, Status

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Url { get; set; }
    }
}