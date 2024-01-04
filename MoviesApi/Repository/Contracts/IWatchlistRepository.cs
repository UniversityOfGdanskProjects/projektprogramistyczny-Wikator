using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;

namespace MoviesApi.Repository.Contracts;

public interface IWatchlistRepository
{
    Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(Guid userId);
    Task<QueryResult> AddToWatchList(Guid userId, Guid movieId);
    Task<QueryResult> RemoveFromWatchList(Guid userId, Guid movieId);
}