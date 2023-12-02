using MoviesApi.Helpers;
using System.Text.Json;

namespace MoviesApi.Extensions;

public static class HttpExtensions
{
	public static void AddPaginationHeader(this HttpResponse response, PaginationHeader paginationHeader)
	{
		JsonSerializerOptions options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationHeader, options));
		response.Headers.Append("Access-Control-Expose-Headers", "Pagination");
	}
}
