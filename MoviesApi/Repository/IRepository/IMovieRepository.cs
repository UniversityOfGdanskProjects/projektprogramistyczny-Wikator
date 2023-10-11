using MoviesApi.Models;

namespace MoviesApi.Repository.IRepository
{
	public interface IMovieRepository
	{
		Task<List<Movie>> GetMovies();

		Task<Movie> AddMovie(Movie movie);
	}
}
