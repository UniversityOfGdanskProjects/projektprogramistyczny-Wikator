using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.Api.Services.Contracts;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;

namespace MoviesService.Api.Controllers;

[Authorize]
[Route("api/movie")]
public class FavouriteController(
    IAsyncQueryExecutor queryExecutor,
    IFavouriteRepository favouriteRepository,
    IMovieRepository movieRepository,
    IUserClaimsProvider claimsProvider) : BaseApiController(queryExecutor)
{
    private IFavouriteRepository FavouriteRepository { get; } = favouriteRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    private IUserClaimsProvider ClaimsProvider { get; } = claimsProvider;


    [HttpGet("favourite")]
    public async Task<IActionResult> GetAllMoviesOnFavouriteList()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var movies = await FavouriteRepository
                .GetAllFavouriteMovies(tx, ClaimsProvider.GetUserId(User));

            return Ok(movies);
        });
    }

    [HttpPost("{movieId:guid}/favourite")]
    public async Task<IActionResult> AddToFavouriteList(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist found");

            var userId = ClaimsProvider.GetUserId(User);

            if (await FavouriteRepository.MovieIsFavourite(tx, movieId, userId))
                return BadRequest("Movie is already favourite");

            await FavouriteRepository.SetMovieAsFavourite(tx, userId, movieId);
            return NoContent();
        });
    }

    [HttpDelete("{movieId:guid}/favourite")]
    public async Task<IActionResult> RemoveFromFavourites(Guid movieId)
    {
        return await QueryExecutor.ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = ClaimsProvider.GetUserId(User);

            if (!await FavouriteRepository.MovieIsFavourite(tx, movieId, userId))
                return BadRequest("This movie is not on your favourite list");

            await FavouriteRepository.UnsetMovieAsFavourite(tx, userId, movieId);
            return NoContent();
        });
    }
}