using Microsoft.AspNetCore.Mvc;
using MoviesService.Controllers.Base;
using MoviesService.DataAccess.Repositories.Contracts;
using Neo4j.Driver;

namespace MoviesService.Controllers;

[Route("api/[controller]")]
public class GenreController(IDriver driver, IGenreRepository genreRepository) : BaseApiController(driver)
{
    private IGenreRepository GenreRepository { get; } = genreRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllGenres()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var genres = await GenreRepository.GetAllGenres(tx);
            return Ok(genres);
        });
    }
}
