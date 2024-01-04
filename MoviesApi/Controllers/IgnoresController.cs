using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class IgnoresController(IIgnoresRepository ignoresRepository) : BaseApiController
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllIgnoredMovies()
    {
        var userId = User.GetUserId();
        var movies = await IgnoresRepository.GetAllIgnoreMovies(userId);

        return Ok(movies);
    }
    
    [HttpPost("{movieId:guid}")]
    public async Task<IActionResult> IgnoreMovie(Guid movieId)
    {
        var userId = User.GetUserId();
        var result = await IgnoresRepository.IgnoreMovie(userId, movieId);

        return result.Status switch
        {
            QueryResultStatus.Completed => Ok(),
            QueryResultStatus.NotFound => NotFound("Movie not found"),
            QueryResultStatus.EntityAlreadyExists => BadRequest("Movie is already ignored"),
            _ => throw new Exception(nameof(result))
        };
    }
    
    [HttpDelete("{movieId:guid}")]
    public async Task<IActionResult> RemoveMovieFromIgnored(Guid movieId)
    {
        var userId = User.GetUserId();
        var result = await IgnoresRepository.RemoveIgnoreMovie(userId, movieId);

        return result.Status switch
        {
            QueryResultStatus.Completed => NoContent(),
            QueryResultStatus.NotFound => NotFound("Movie does not exist"),
            QueryResultStatus.RelationDoesNotExist => BadRequest("This movie is not on your ignored list"),
            _ => throw new Exception(nameof(result))
        };
    }
}
