using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IFavouriteRepository
{
    Task<IEnumerable<MovieDto>> GetAllFavouriteMovies(IAsyncQueryRunner tx, Guid userId);
    Task SetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task UnsetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task<bool> MovieIsFavourite(IAsyncQueryRunner tx, Guid movieId, Guid userId);
}