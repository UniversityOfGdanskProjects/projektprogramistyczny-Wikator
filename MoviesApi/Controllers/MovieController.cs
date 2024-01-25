using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Exceptions;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Authorize(Policy = "RequireAdminRole")]
[Route("api/[controller]")]
public class MovieController(IDriver driver, IMovieRepository movieRepository,
	IPhotoService photoService) : BaseApiController(driver)
{
	private IMovieRepository MovieRepository { get; } = movieRepository;
	private IPhotoService PhotoService { get; } = photoService;

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetMovies([FromQuery] MovieQueryParams queryParams)
	{
		return await ExecuteReadAsync(async tx =>
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var pagedList = userId switch
			{
				null => await MovieRepository.GetMoviesWhenNotLoggedIn(tx, queryParams),
				_ => await MovieRepository.GetMoviesExcludingIgnored(tx, Guid.Parse(userId), queryParams)
			};

			PaginationHeader paginationHeader = new(pagedList.CurrentPage, pagedList.PageSize, pagedList.TotalCount,
				pagedList.TotalPages);

			Response.AddPaginationHeader(paginationHeader);
			return Ok(pagedList);
		});
	}
	
	[HttpGet("{id:guid}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetMovie(Guid id)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var movie = userId switch
			{
				null => await MovieRepository.GetMovieDetails(tx, id),
				_ => await MovieRepository.GetMovieDetails(tx, id, Guid.Parse(userId))
			};
		
			return movie is null ? NotFound() : Ok(movie);
		});
	}

	[HttpPost]
	public async Task<IActionResult> CreateMovie(AddMovieDto movieDto)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			string? pictureAbsoluteUri = null;
			string? picturePublicId = null;

			if (movieDto.FileContent is not null)
			{
				var file = new FormFile(
					new MemoryStream(movieDto.FileContent),
					0,
					movieDto.FileContent.Length,
					"file", $"movie-{new Guid()}"
					);

				var uploadResult = await PhotoService.AddPhotoAsync(file);
				if (uploadResult.Error is not null)
					throw new PhotoServiceException("Photo failed to save, please try again in few minutes");

				pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
				picturePublicId = uploadResult.PublicId;
			}
			
			var movie = await MovieRepository.AddMovie(tx, movieDto, pictureAbsoluteUri, picturePublicId);
			return CreatedAtAction(nameof(GetMovies), movie);
		});
	}
	
	[HttpPut("{id:guid}")]
	public async Task<IActionResult> EditMovie(Guid id, EditMovieDto movieDto)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			if (!await MovieRepository.MovieExists(tx, id))
				return NotFound("Movie does not exist");
			
			var movie = await MovieRepository.EditMovie(tx, id, User.GetUserId(), movieDto);
			return Ok(movie);
		});
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> DeleteMovie(Guid id)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			if (!await MovieRepository.MovieExists(tx, id))
				return NotFound("Movie does not exist");
			
			var publicId = await MovieRepository.GetPublicId(tx, id);
			if (publicId is not null)
			{
				var deleteResult = await PhotoService.DeleteAsync(publicId);
				if (deleteResult.Error is not null)
					throw new PhotoServiceException("Photo failed to delete, please try again in few minutes");
			}

			await MovieRepository.DeleteMovie(tx, id);
			return NoContent();
		});
	}
	
	[HttpPost("{id:guid}/picture")]
	public async Task<IActionResult> AddMoviePicture(Guid id, UpsertPictureDto pictureDto)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			if (!await MovieRepository.MovieExists(tx, id))
				return NotFound("Movie does not exist");
			
			if (await MovieRepository.MoviePictureExists(tx, id))
				return BadRequest("Movie already has a picture");
			
			var file = new FormFile(
				new MemoryStream(pictureDto.FileContent),
				0,
				pictureDto.FileContent.Length,
				"file", $"movie-{new Guid()}"
			);

			var uploadResult = await PhotoService.AddPhotoAsync(file);
			if (uploadResult.Error is not null)
				throw new PhotoServiceException("Photo failed to save, please try again in few minutes");

			var pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
			var picturePublicId = uploadResult.PublicId;
			
			await MovieRepository.AddMoviePicture(tx, id, pictureAbsoluteUri, picturePublicId);
			return Ok(new {pictureUri = pictureAbsoluteUri});
		});
	}

	[HttpPut("{id:guid}/picture")]
	public async Task<IActionResult> EditMoviePicture(Guid id, UpsertPictureDto pictureDto)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			if (!await MovieRepository.MovieExists(tx, id))
				return NotFound("Movie does not exist");

			var publicId = await MovieRepository.GetPublicId(tx, id);

			if (publicId is not null)
			{
				var deleteResult = await PhotoService.DeleteAsync(publicId);
				if (deleteResult.Error is not null)
					throw new PhotoServiceException("Photo failed to delete, please try again in few minutes");
			}

			var file = new FormFile(
				new MemoryStream(pictureDto.FileContent),
				0,
				pictureDto.FileContent.Length,
				"file", $"movie-{new Guid()}"
			);

			var uploadResult = await PhotoService.AddPhotoAsync(file);
			if (uploadResult.Error is not null)
				throw new PhotoServiceException("Photo failed to save, please try again in few minutes");

			var pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
			var picturePublicId = uploadResult.PublicId;

			await MovieRepository.AddMoviePicture(tx, id, pictureAbsoluteUri, picturePublicId);
			return Ok(new {pictureUri = pictureAbsoluteUri});
		});
	}
	
	[HttpDelete("{id:guid}/picture")]
	public async Task<IActionResult> DeleteMoviePicture(Guid id)
	{
		return await ExecuteWriteAsync(async tx =>
		{
			if (!await MovieRepository.MovieExists(tx, id))
				return NotFound("Movie does not exist");

			var publicId = await MovieRepository.GetPublicId(tx, id);

			if (publicId is null)
				return BadRequest("Movie does not have a picture");

			var deleteResult = await PhotoService.DeleteAsync(publicId);
			if (deleteResult.Error is not null)
				throw new PhotoServiceException("Photo failed to delete, please try again in few minutes");

			await MovieRepository.DeleteMoviePicture(tx, id);
			return NoContent();
		});
	}
}
