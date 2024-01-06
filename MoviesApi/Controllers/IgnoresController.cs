using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Controllers;

[Authorize]
public class IgnoresController(IIgnoresRepository ignoresRepository, IUserClaimsProvider userClaimsProvider)
    : BaseApiController
{
    private IIgnoresRepository IgnoresRepository { get; } = ignoresRepository;
    private IUserClaimsProvider UserClaimsProvider { get; } = userClaimsProvider;

    [HttpGet]
    public async Task<IActionResult> GetAllIgnoredMovies()
    {
        var movies = await IgnoresRepository.GetAllIgnoreMovies(UserClaimsProvider.GetUserId(User));

        return Ok(movies);
    }
    
    [HttpPost("{movieId:guid}")]
    public async Task<IActionResult> IgnoreMovie(Guid movieId)
    {
        var result = await IgnoresRepository.IgnoreMovie(UserClaimsProvider.GetUserId(User), movieId);

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
        var result = await IgnoresRepository.RemoveIgnoreMovie(UserClaimsProvider.GetUserId(User), movieId);

        return result.Status switch
        {
            QueryResultStatus.Completed => NoContent(),
            QueryResultStatus.NotFound => NotFound("Movie does not exist"),
            QueryResultStatus.RelationDoesNotExist => BadRequest("This movie is not on your ignored list"),
            _ => throw new Exception(nameof(result))
        };
    }
}
