using MoviesService.DataAccess.Helpers;
using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using MoviesService.Models.Parameters;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IMovieRepository
{
    Task<bool> MovieExists(IAsyncQueryRunner tx, Guid movieId);
    Task<MovieDetailsDto?> GetMovieDetails(IAsyncQueryRunner tx, Guid movieId, Guid? userId = null);
    Task<PagedList<MovieDto>> GetMoviesWhenNotLoggedIn(IAsyncQueryRunner tx, MovieQueryParams queryParams);

    Task<PagedList<MovieDto>>
        GetMoviesExcludingIgnored(IAsyncQueryRunner tx, Guid userId, MovieQueryParams queryParams);

    Task<MovieDetailsDto> AddMovie(IAsyncQueryRunner tx, AddMovieDto movieDto, string? pictureAbsoluteUri,
        string? picturePublicId);

    Task<MovieDetailsDto> EditMovie(IAsyncQueryRunner tx, Guid movieId, Guid userId, EditMovieDto movieDto);
    Task<bool> MoviePictureExists(IAsyncQueryRunner tx, Guid movieId);
    Task DeleteMoviePicture(IAsyncQueryRunner tx, Guid movieId);
    Task AddMoviePicture(IAsyncQueryRunner tx, Guid movieId, string pictureAbsoluteUri, string picturePublicId);
    Task DeleteMovie(IAsyncQueryRunner tx, Guid movieId);
    Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid movieId);
    Task<string> GetMostPopularMovieTitle(IAsyncQueryRunner tx);
}