using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Helpers;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<IEnumerable<MovieDto>> GetMovies(MovieQueryParams queryParams);
	Task<IEnumerable<MovieDto>> GetMoviesExcludingIgnored(int userId, MovieQueryParams queryParams);
	Task<MovieDto?> AddMovie(AddMovieDto movieDto);
	Task<QueryResult> DeleteMovie(int movieId);
}