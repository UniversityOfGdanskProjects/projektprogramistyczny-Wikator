using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<IEnumerable<MovieDto>> GetMovies();
	Task<IEnumerable<MovieDto>> GetMoviesExcludingIgnored(int userId);
	Task<MovieDto?> AddMovie(AddMovieDto movieDto);
}