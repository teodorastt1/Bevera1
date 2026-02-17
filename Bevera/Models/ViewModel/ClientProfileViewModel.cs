using System;
using System.Collections.Generic;

namespace Bevera.Models.ViewModels
{
    public class ClientProfileViewModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string Address { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public List<OrderRowVm> Orders { get; set; } = new();
    }

    public class OrderRowVm
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "";
    }
}
