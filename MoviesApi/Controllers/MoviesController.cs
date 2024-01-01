﻿using System.Security.Claims;
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
		var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return userId switch
		{
			null => Ok(await MovieRepository.GetMovies()),
			_ => Ok(await MovieRepository.GetMoviesExcludingIgnored(int.Parse(userId)))
		};
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