using MoviesService.Models.Headers;

namespace MoviesService.Api.Services.Contracts;

public interface IResponseHandler
{
    void AddPaginationHeader(HttpResponse response, PaginationHeader paginationHeader);
}