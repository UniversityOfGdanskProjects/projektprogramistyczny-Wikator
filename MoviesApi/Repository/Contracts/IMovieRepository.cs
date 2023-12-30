using MoviesApi.DTOs;
using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<IEnumerable<MovieDto>> GetMovies();

	Task<MovieDto?> AddMovie(AddMovieDto movieDto);
}