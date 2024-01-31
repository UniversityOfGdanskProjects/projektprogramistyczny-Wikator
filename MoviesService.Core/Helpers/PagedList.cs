namespace MoviesService.Core.Helpers;

public class PagedList<T> : List<T>
{
	public PagedList(IEnumerable<T> items, int currentPage, int pageSize, int totalCount)
	{
		CurrentPage = currentPage;
		TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
		PageSize = pageSize;
		TotalCount = totalCount;
		AddRange(items);
	}

	public int CurrentPage { get; init; }
	public int TotalPages { get; init; }
	public int PageSize { get; init; }
	public int TotalCount { get; init; }
}