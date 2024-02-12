using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/movie")]
public class WatchlistController(
    IAsyncQueryExecutor queryExecutor,
    IWatchlistRepository watchlistRepository,
    IMovieRepository movieRepository,
    IUserClaimsProvider claimsProvider) : BaseApiController(queryExecutor)
{
    private IWatchlistRepository WatchlistRepository { get; } = watchlistRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider ClaimsProvider { get; } = claimsProvider;


    [HttpGet("watchlist")]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var movies = await WatchlistRepository
                .GetAllMoviesOnWatchlist(tx, ClaimsProvider.GetUserId(User));

            return Ok(movies);
        });
    }

    [HttpPost("{movieId:guid}/watchlist")]
    public async Task<IActionResult> AddToWatchList(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist found");

            var userId = ClaimsProvider.GetUserId(User);

            if (await WatchlistRepository.WatchlistExists(tx, movieId, userId))
                return BadRequest("Movie already in watchlist");

            await WatchlistRepository.AddToWatchList(tx, userId, movieId);
            return NoContent();
        });
    }

    [HttpDelete("{movieId:guid}/watchlist")]
    public async Task<IActionResult> RemoveFromWatchList(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = ClaimsProvider.GetUserId(User);

            if (!await WatchlistRepository.WatchlistExists(tx, movieId, userId))
                return BadRequest("This movie is not on your watchlist");

            await WatchlistRepository.RemoveFromWatchList(tx, userId, movieId);
            return NoContent();
        });
    }
}