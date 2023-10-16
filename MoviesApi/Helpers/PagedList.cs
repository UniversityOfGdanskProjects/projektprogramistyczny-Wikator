namespace MoviesApi.Helpers
{
	public class PagedList<T> : List<T>
	{
		public PagedList(List<T> items, int currentPage, int pageSize, int totalCount)
		{
			CurrentPage = currentPage;
			TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			PageSize = pageSize;
			TotalCount = totalCount;
			AddRange(items);
		}

		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
	}
}
