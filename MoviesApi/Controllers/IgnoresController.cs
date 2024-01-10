using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Authorize]
public class IgnoresController(IDriver driver, IIgnoresRepository ignoresRepository,
    IUserClaimsProvider userClaimsProvider, IMovieRepository movieRepository)
    : BaseApiController(driver)
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = userClaimsProvider;

    [HttpGet]
    public async Task<IActionResult> GetAllIgnoredMovies()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var movies = await IgnoresRepository.GetAllIgnoreMovies(tx, UserClaimsProvider.GetUserId(User));
            return Ok(movies);
        });
    }
    
    [HttpPost("{movieId:guid}")]
    public async Task<IActionResult> IgnoreMovie(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = UserClaimsProvider.GetUserId(User);

            if (await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is already ignored");

            await IgnoresRepository.IgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }
    
    [HttpDelete("{movieId:guid}")]
    public async Task<IActionResult> RemoveMovieFromIgnored(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = UserClaimsProvider.GetUserId(User);

            if (!await IgnoresRepository.IgnoresExists(tx, movieId, userId))
                return BadRequest("Movie is not on your ignored list");

            await IgnoresRepository.RemoveIgnoreMovie(tx, userId, movieId);
            return NoContent();
        });
    }
}
