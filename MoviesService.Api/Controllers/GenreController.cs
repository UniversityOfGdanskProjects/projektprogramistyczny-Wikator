using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;

namespace MoviesService.Api.Controllers;

[Route("api/[controller]")]
public class GenreController(IAsyncQueryExecutor queryExecutor, IGenreRepository genreRepository)
    : BaseApiController(queryExecutor)
{
    private IGenreRepository GenreRepository { get; } = genreRepository;

    [HttpGet]
    public async Task<IActionResult> GetAllGenres()
    {
        return await QueryExecutor.ExecuteReadAsync<IActionResult>(async tx =>
        {
            var genres = await GenreRepository.GetAllGenres(tx);
            return Ok(genres);
        });
    }
}