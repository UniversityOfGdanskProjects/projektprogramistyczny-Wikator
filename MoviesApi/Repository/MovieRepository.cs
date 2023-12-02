using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class MovieRepository(IDriver driver) : IMovieRepository
{
	private IDriver Driver { get; } = driver;
		
	public async Task<Movie> AddMovie(Movie movie)
	{
		var session = Driver.AsyncSession();

		try
		{
			await session.RunAsync($"CREATE (a:Movie {{ Title: \"${movie.Title}\", Description: \"${movie.Description}\" }})");
		}
		finally
		{
			await session.CloseAsync();
		}
		return movie;
	}

	public async Task<List<Movie>> GetMovies()
	{
		List<Movie> movies;
		var session = Driver.AsyncSession();
		try
		{
			const string query = """
			                     MATCH (a:Movie)
			                     RETURN a.Title as title, a.Description as description
			                     LIMIT 10
			                     """;
			var cursor = await session.RunAsync(query);
			movies = await cursor.ToListAsync(record => new Movie
			{
				Title = record["title"].As<string>(),
				Description = record["description"].As<string>()
			});
		}
		finally
		{
			await session.CloseAsync();
		}
		return movies;
	}
}