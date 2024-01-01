using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

public class IgnoresController(IIgnoresRepository ignoresRepository) : BaseApiController
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        var userId = User.GetUserId();
        var movies = await IgnoresRepository.GetAllIgnoreMovies(userId);

        return Ok(movies);
    }
    
    [HttpPost("{movieId:int}")]
    public async Task<IActionResult> AddToWatchList(int movieId)
    {
        var userId = User.GetUserId();
        var result = await IgnoresRepository.IgnoreMovie(userId, movieId);

        return result switch
        {
            QueryResult.Completed => Ok(),
            QueryResult.NotFound => NotFound("Movie not found"),
            QueryResult.EntityAlreadyExists => BadRequest("Movie is already ignored"),
            _ => throw new Exception(nameof(result))
        };
    }
    
    [HttpDelete("{movieId:int}")]
    public async Task<IActionResult> RemoveFromWatchList(int movieId)
    {
        var userId = User.GetUserId();
        var result = await IgnoresRepository.RemoveIgnoreMovie(userId, movieId);

        return result switch
        {
            QueryResult.Completed => NoContent(),
            QueryResult.NotFound => NotFound("Movie does not exist in ignore list"),
            _ => throw new Exception(nameof(result))
        };
    }
}