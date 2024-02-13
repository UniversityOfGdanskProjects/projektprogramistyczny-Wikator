namespace MoviesService.DataAccess.Helpers;

public class PagedList<T>(IEnumerable<T> items, int currentPage, int pageSize, int totalCount)
{
    public IEnumerable<T> Items { get; } = items;
    public int CurrentPage { get; } = currentPage;
    public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
    public int PageSize { get; } = pageSize;
    public int TotalCount { get; } = totalCount;
}
