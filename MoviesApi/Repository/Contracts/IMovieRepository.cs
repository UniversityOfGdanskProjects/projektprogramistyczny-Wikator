using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<IEnumerable<MovieDto>> GetMovies();
	Task<MovieDto?> AddMovie(AddMovieDto movieDto);
}