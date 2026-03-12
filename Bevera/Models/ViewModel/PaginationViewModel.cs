namespace Bevera.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public string Controller { get; set; } = "";
        public string Action { get; set; } = "";
        public Dictionary<string, string?> RouteValues { get; set; } = new();
    }
}