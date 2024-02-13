namespace MoviesService.Models.Parameters;

public class NotificationQueryParams
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 30;
}