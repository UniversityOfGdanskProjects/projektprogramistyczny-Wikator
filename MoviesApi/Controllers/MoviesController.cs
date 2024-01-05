using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Exceptions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

public class MoviesController(IMovieRepository movieRepository) : BaseApiController
{
	private IMovieRepository MovieRepository { get; } = movieRepository;
		
	[HttpGet]
	public async Task<IActionResult> GetMovies([FromQuery] MovieQueryParams queryParams)
	{
		var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return userId switch
		{
			null => Ok(await MovieRepository.GetMovies(queryParams)),
			_ => Ok(await MovieRepository.GetMoviesExcludingIgnored(Guid.Parse(userId), queryParams))
		};
	}
	
	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetMovies(Guid id)
	{
		var movie = await MovieRepository.GetMovieDetails(id);
		return movie is null ? NotFound() : Ok(movie);
	}

	[HttpPost]
	public async Task<IActionResult> CreateMovie(AddMovieDto movieDto)
	{
		var movie = await MovieRepository.AddMovie(movieDto);

		return movie.Status switch
		{
			QueryResultStatus.PhotoFailedToSave => throw new PhotoServiceException("Photo failed to save, please try again in few minutes"),
			QueryResultStatus.Completed => CreatedAtAction(nameof(GetMovies), movie.Data),
			_ => throw new Exception("Unexpected status returned")
		};
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteMovie(Guid id)
	{
		var result = await MovieRepository.DeleteMovie(id);
		
		return result.Status switch
		{
			QueryResultStatus.NotFound => NotFound("Movie does not exist"),
			QueryResultStatus.PhotoFailedToDelete => throw new PhotoServiceException("Photo failed to delete, please try again in few minutes"),
			QueryResultStatus.Completed => NoContent(),
			_ => throw new Exception("This shouldn't have happened")
		};
	}
}
