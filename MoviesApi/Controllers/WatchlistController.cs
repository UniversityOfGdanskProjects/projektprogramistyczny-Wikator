using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class WatchlistController(IWatchlistRepository watchlistRepository) : BaseApiController
{
    private IWatchlistRepository WatchlistRepository { get; } = watchlistRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        var userId = User.GetUserId();
        var movies = await WatchlistRepository.GetAllMoviesOnWatchlist(userId);
    
        return Ok(movies);
    }
    
    [HttpPost("{movieId:guid}")]
    public async Task<IActionResult> AddToWatchList(Guid movieId)
    {
        var userId = User.GetUserId();
        var result = await WatchlistRepository.AddToWatchList(userId, movieId);
    
        return result.Status switch
        {
            QueryResultStatus.Completed => Ok(),
            QueryResultStatus.NotFound => NotFound("Movie not found"),
            QueryResultStatus.EntityAlreadyExists => BadRequest("Movie already in watchlist"),
            _ => throw new Exception(nameof(result.Status))
        };
    }
    
    [HttpDelete("{movieId:guid}")]
    public async Task<IActionResult> RemoveFromWatchList(Guid movieId)
    {
        var userId = User.GetUserId();
        var result = await WatchlistRepository.RemoveFromWatchList(userId, movieId);
    
        return result.Status switch
        {
            QueryResultStatus.Completed => NoContent(),
            QueryResultStatus.NotFound => NotFound("Movie or watchlist not found"),
            _ => throw new Exception(nameof(result))
        };
    }
}