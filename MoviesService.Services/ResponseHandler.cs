using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MoviesService.Core.Helpers;
using MoviesService.Services.Contracts;

namespace MoviesService.Services;

public class ResponseHandler : IResponseHandler
{
    public void AddPaginationHeader(HttpResponse response, PaginationHeader paginationHeader)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationHeader, options));
        response.Headers.Append("Access-Control-Expose-Headers", "Pagination");
    }
}
