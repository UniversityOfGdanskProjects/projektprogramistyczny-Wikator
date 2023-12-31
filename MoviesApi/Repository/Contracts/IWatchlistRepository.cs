using MoviesApi.DTOs;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IWatchlistRepository
{
    Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(int userId);
    Task<QueryResult> AddToWatchList(int userId, int movieId);
    Task<QueryResult> RemoveFromWatchList(int userId, int movieId);
}