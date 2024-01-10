using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IWatchlistRepository
{
    Task<IEnumerable<MovieDetailsDto>> GetAllMoviesOnWatchlist(IAsyncQueryRunner tx, Guid userId);
    Task AddToWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task RemoveFromWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task<bool> WatchlistExists(IAsyncQueryRunner tx, Guid movieId, Guid userId);
}