using System.Text.Json;
using MoviesService.Api.Services.Contracts;
using MoviesService.Models.Headers;

namespace MoviesService.Api.Services;

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