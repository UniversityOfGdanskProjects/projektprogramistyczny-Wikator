using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class WatchlistController(IWatchlistRepository watchlistRepository, IUserClaimsProvider userClaimsProvider)
    : BaseApiController
{
    private IWatchlistRepository WatchlistRepository { get; } = watchlistRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = userClaimsProvider;

    [HttpGet]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        var movies = await WatchlistRepository
            .GetAllMoviesOnWatchlist(UserClaimsProvider.GetUserId(User));
    
        return Ok(movies);
    }
    
    [HttpPost("{movieId:guid}")]
    public async Task<IActionResult> AddToWatchList(Guid movieId)
    {
        var result = await WatchlistRepository.AddToWatchList(UserClaimsProvider.GetUserId(User), movieId);
    
        return result.Status switch
        {
            QueryResultStatus.Completed => NoContent(),
            QueryResultStatus.NotFound => NotFound("Movie not found"),
            QueryResultStatus.EntityAlreadyExists => BadRequest("Movie already in watchlist"),
            _ => throw new Exception(nameof(result.Status))
        };
    }
    
    [HttpDelete("{movieId:guid}")]
    public async Task<IActionResult> RemoveFromWatchList(Guid movieId)
    {
        var result = await WatchlistRepository.RemoveFromWatchList(UserClaimsProvider.GetUserId(User), movieId);
    
        return result.Status switch
        {
            QueryResultStatus.Completed => NoContent(),
            QueryResultStatus.NotFound => NotFound("Movie or watchlist not found"),
            _ => throw new Exception(nameof(result))
        };
    }
}
