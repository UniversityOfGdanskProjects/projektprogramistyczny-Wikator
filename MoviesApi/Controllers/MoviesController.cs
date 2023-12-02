using Microsoft.AspNetCore.Mvc;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class MoviesController(IMovieRepository movieRepository) : ControllerBase
{
	private IMovieRepository MovieRepository { get; } = movieRepository;
		
	[HttpGet]
	public async Task<ActionResult<List<Movie>>> GetMovies()
	{
		var movies = await MovieRepository.GetMovies();

		return Ok(movies);
	}

	[HttpPost]
	public async Task<ActionResult<Movie>> CreateMovie(Movie movie)
	{
		await MovieRepository.AddMovie(movie);

		return CreatedAtAction(nameof(GetMovies), movie);
	}
}