using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Authorize]
[Route("api/movie")]
public class WatchlistController(IDriver driver, IWatchlistRepository watchlistRepository,
    IMovieRepository movieRepository) : BaseApiController(driver)
{
    private IWatchlistRepository WatchlistRepository { get; } = watchlistRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    

    [HttpGet("watchlist")]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var movies = await WatchlistRepository
                .GetAllMoviesOnWatchlist(tx, User.GetUserId());

            return Ok(movies);
        });
    }
    
    [HttpPost("{movieId:guid}/watchlist")]
    public async Task<IActionResult> AddToWatchList(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist found");

            var userId = User.GetUserId();

            if (await WatchlistRepository.WatchlistExists(tx, movieId, userId))
                return BadRequest("Movie already in watchlist");

            await WatchlistRepository.AddToWatchList(tx, userId, movieId);
            return NoContent();
        });
    }
    
    [HttpDelete("{movieId:guid}/watchlist")]
    public async Task<IActionResult> RemoveFromWatchList(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = User.GetUserId();

            if (!await WatchlistRepository.WatchlistExists(tx, movieId, userId))
                return BadRequest("This movie is not on your watchlist");

            await WatchlistRepository.RemoveFromWatchList(tx, userId, movieId);
            return NoContent();
        });
    }
}
