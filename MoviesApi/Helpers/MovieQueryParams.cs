using MoviesApi.Enums;

namespace MoviesApi.Helpers;

public class MovieQueryParams
{
    public string Title { get; init; } = string.Empty;
    public Guid? Actor { get; init; }
    public string? SortBy { get; init; }
    public SortOrder SortOrder { get; init; } = SortOrder.Ascending;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 6;
}
