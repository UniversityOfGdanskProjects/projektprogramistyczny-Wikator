using MoviesApi.Helpers;
using System.Text.Json;

namespace MoviesApi.Extensions
{
	public static class HttpExtensions
	{
		public static void AddPaginationHeader(this HttpResponse response, PaginationHeader paginationHeader)
		{
			JsonSerializerOptions options = new()
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			response.Headers.Add("Pagination", JsonSerializer.Serialize(paginationHeader, options));
			response.Headers.Add("Access-Control-Expose-Headers", "Pagination");
		}
	}
}
