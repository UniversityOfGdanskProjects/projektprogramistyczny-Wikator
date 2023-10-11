using Microsoft.AspNetCore.Mvc;
using MoviesApi.Models;
using MoviesApi.Repository.IRepository;

namespace MoviesApi.Controllers
{
	[ApiController]
	[Route("/api/[controller]")]
	public class MoviesController : ControllerBase
	{
		private readonly IMovieRepository _movieRepository;

        public MoviesController(IMovieRepository movieRepository)
        {
			_movieRepository = movieRepository;

		}

		[HttpGet]
		public async Task<ActionResult<List<Movie>>> GetMovies()
		{
			List<Movie> movies = await _movieRepository.GetMovies();

			return Ok(movies);
		}

		[HttpPost]
		public async Task<ActionResult<Movie>> CreateMovie(Movie movie)
		{
			await _movieRepository.AddMovie(movie);

			return CreatedAtAction(nameof(GetMovies), movie);
		}
    }
}
