using MoviesService.Models.Enums;

namespace MoviesService.Models.Parameters;

public class MovieQueryParams
{
    public string Title { get; init; } = string.Empty;
    public Guid? Actor { get; init; }
    public string? Genre { get; init; }
    public bool? InTheaters { get; init; }
    public SortBy SortBy { get; init; } = SortBy.Popularity;
    public SortOrder SortOrder { get; init; } = SortOrder.Ascending;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 6;
}