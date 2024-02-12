using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/movie")]
public class IgnoredController(
    IAsyncQueryExecutor queryExecutor,
    IIgnoresRepository ignoresRepository,
    IMovieRepository movieRepository,
    IUserClaimsProvider claimsProvider) : BaseApiController(queryExecutor)
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider ClaimsProvider { get; } = claimsProvider;


    [HttpGet("ignored")]
    public async Task<IActionResult> GetAllIgnoredMovies()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var movies = await IgnoresRepository.GetAllIgnoreMovies(tx, ClaimsProvider.GetUserId(User));
            return Ok(movies);
        });
    }

    [HttpPost("{movieId:guid}/ignored")]
    public async Task<IActionResult> IgnoreMovie(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = ClaimsProvider.GetUserId(User);

            if (await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is already ignored");

            await IgnoresRepository.IgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }

    [HttpDelete("{movieId:guid}/ignored")]
    public async Task<IActionResult> RemoveMovieFromIgnored(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = ClaimsProvider.GetUserId(User);

            if (!await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is not on your ignored list");

            await IgnoresRepository.RemoveIgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }
}