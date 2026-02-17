using System.Collections.Generic;

namespace Bevera.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }

        public string Action { get; set; } = "Index";
        public string Controller { get; set; } = "";

        // query params които искаш да се запазят (q, role и т.н.)
        public Dictionary<string, string?> RouteValues { get; set; } = new();
    }
}
