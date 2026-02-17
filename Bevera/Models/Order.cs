using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = "";
        public ApplicationUser Client { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // IMPORTANT:
        // Status е string при теб, затова го държим синхронизиран с имената на enum-а:
        // Draft, Submitted, Preparing, ReadyForPickup, Delivered, Received, Cancelled
        [Required, StringLength(20)]
        public string Status { get; set; } = "Draft";

        // PaymentStatus също е string: Unpaid/Paid (ще добавим константи)
        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [Range(0, 99999999)]
        public decimal Total { get; set; }

        [Required, StringLength(120)]
        public string FullName { get; set; } = "";

        [Required, StringLength(120)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

        // Invoice metadata
        public string? InvoiceFileName { get; set; }
        public string? InvoiceStoredFileName { get; set; }
        public string? InvoiceContentType { get; set; }
        public long? InvoiceFileSize { get; set; }
        public DateTime? InvoiceCreatedAt { get; set; }

        public DateTime? PaidOn { get; set; }
    }
}
