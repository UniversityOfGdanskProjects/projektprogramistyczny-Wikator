using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId);
	Task<MovieDetailsDto?> GetMovieDetails(Guid movieId, Guid? userId = null);
	Task<PagedList<MovieDto>> GetMoviesWhenNotLoggedIn(MovieQueryParams queryParams);
	Task<PagedList<MovieDto>> GetMoviesExcludingIgnored(Guid userId, MovieQueryParams queryParams);
	Task<QueryResult<MovieDetailsDto>> AddMovie(AddMovieDto movieDto);
	Task<QueryResult> DeleteMovie(Guid movieId);
}