using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Helpers;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId);
	Task<IEnumerable<MovieDto>> GetMovies(MovieQueryParams queryParams);
	Task<IEnumerable<MovieDto>> GetMoviesExcludingIgnored(Guid userId, MovieQueryParams queryParams);
	Task<MovieDto?> AddMovie(AddMovieDto movieDto);
	Task<QueryResult> DeleteMovie(Guid movieId);
}