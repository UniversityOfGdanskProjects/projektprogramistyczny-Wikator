using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IMovieRepository
{
	Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId);
	Task<MovieDetailsDto?> GetMovieDetails(IAsyncQueryRunner tx,Guid movieId, Guid? userId = null);
	Task<PagedList<MovieDto>> GetMoviesWhenNotLoggedIn(IAsyncQueryRunner tx,MovieQueryParams queryParams);
	Task<PagedList<MovieDto>> GetMoviesExcludingIgnored(IAsyncQueryRunner tx,Guid userId, MovieQueryParams queryParams);
	Task<MovieDetailsDto> AddMovie(IAsyncQueryRunner tx,AddMovieDto movieDto, string? pictureAbsoluteUri, string? picturePublicId);
	Task<MovieDetailsDto> EditMovie(IAsyncQueryRunner tx, Guid movieId, EditMovieDto movieDto);
	Task DeleteMovie(IAsyncQueryRunner tx,Guid movieId);
	Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid movieId);
}