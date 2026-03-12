namespace Bevera.Models.ViewModels
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }

        public int TotalPages =>
            PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}