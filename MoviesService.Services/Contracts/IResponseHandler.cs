using Microsoft.AspNetCore.Http;
using MoviesService.Core.Helpers;

namespace MoviesService.Services.Contracts;

public interface IResponseHandler
{
    void AddPaginationHeader(HttpResponse response, PaginationHeader paginationHeader);
}