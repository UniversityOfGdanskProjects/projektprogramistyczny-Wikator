using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<List<Movie>> GetMovies();

	Task<Movie> AddMovie(Movie movie);
}