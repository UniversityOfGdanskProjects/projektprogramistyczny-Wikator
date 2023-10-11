using MoviesApi.Models;
using MoviesApi.Repository.IRepository;
using Neo4j.Driver;

namespace MoviesApi.Repository
{
	public class MovieRepository : IMovieRepository
	{
		private readonly IDriver _driver;

        public MovieRepository(IDriver driver)
        {
            _driver = driver;
        }

        public async Task<Movie> AddMovie(Movie movie)
		{
			IAsyncSession session = _driver.AsyncSession();

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
			List<Movie> movies = new();
			IAsyncSession session = _driver.AsyncSession();
			try
			{
				IResultCursor cursor = await session.RunAsync(@"MATCH (a:Movie)
				RETURN a.Title as title, a.Description as description
				LIMIT 10");
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
}
