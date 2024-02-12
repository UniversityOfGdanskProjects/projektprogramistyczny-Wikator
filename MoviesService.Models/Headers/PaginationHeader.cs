namespace MoviesService.Models.Headers;

public class PaginationHeader(int currentPage, int itemsPerPage, int totalItems, int totalPages)
{
    public int CurrentPage { get; } = currentPage;
    public int ItemsPerPage { get; } = itemsPerPage;
    public int TotalItems { get; } = totalItems;
    public int TotalPages { get; } = totalPages;
}