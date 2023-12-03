using MoviesApi.DTOs;
using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<List<Movie>> GetMovies();

	Task<Movie?> AddMovie(AddMovieDto movieDto);
}