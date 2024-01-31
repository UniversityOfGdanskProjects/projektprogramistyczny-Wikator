using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Controllers.Base;
using MoviesService.Core.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using Neo4j.Driver;

namespace MoviesService.Controllers;

[Authorize]
[Route("api/movie")]
public class IgnoredController(IDriver driver, IIgnoresRepository ignoresRepository,
    IMovieRepository movieRepository) : BaseApiController(driver)
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;

    
    [HttpGet("ignored")]
    public async Task<IActionResult> GetAllIgnoredMovies()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var movies = await IgnoresRepository.GetAllIgnoreMovies(tx, User.GetUserId());
            return Ok(movies);
        });
    }
    
    [HttpPost("{movieId:guid}/ignored")]
    public async Task<IActionResult> IgnoreMovie(Guid movieId)
    {
        return await ExecuteWriteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = User.GetUserId();

            if (await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is already ignored");

            await IgnoresRepository.IgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }
    
    [HttpDelete("{movieId:guid}/ignored")]
    public async Task<IActionResult> RemoveMovieFromIgnored(Guid movieId)
    {
        return await ExecuteWriteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = User.GetUserId();

            if (!await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is not on your ignored list");

            await IgnoresRepository.RemoveIgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }
}
