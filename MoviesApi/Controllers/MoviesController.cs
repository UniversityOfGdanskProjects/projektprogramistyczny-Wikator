using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MoviesController(IMovieRepository movieRepository) : ControllerBase
{
	private IMovieRepository MovieRepository { get; } = movieRepository;
		
	[HttpGet]
	public async Task<IActionResult> GetMovies()
	{
		var movies = await MovieRepository.GetMovies();

		return Ok(movies);
	}

	[HttpPost]
	public async Task<IActionResult> CreateMovie(AddMovieDto movieDto)
	{
		var movie = await MovieRepository.AddMovie(movieDto);
			
		if (movie is null)
			return BadRequest("The movie could not be created.");

		return CreatedAtAction(nameof(GetMovies), movie);
	}
}