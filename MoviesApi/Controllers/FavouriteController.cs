﻿using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/movie")]
public class FavouriteController(IDriver driver, IFavouriteRepository favouriteRepository,
    IMovieRepository movieRepository) : BaseApiController(driver)
{
    private IFavouriteRepository FavouriteRepository { get; } = favouriteRepository;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
    
    [HttpGet("favourite")]
    public async Task<IActionResult> GetAllMoviesOnFavouriteList()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var movies = await FavouriteRepository
                .GetAllFavouriteMovies(tx, User.GetUserId());

            return Ok(movies);
        });
    }
    
    [HttpPost("{movieId:guid}/favourite")]
    public async Task<IActionResult> AddToFavouriteList(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist found");

            var userId = User.GetUserId();

            if (await FavouriteRepository.MovieIsFavourite(tx, movieId, userId))
                return BadRequest("Movie is already favourite");

            await FavouriteRepository.SetMovieAsFavourite(tx, userId, movieId);
            return NoContent();
        });
    }
    
    [HttpDelete("{movieId:guid}/favourite")]
    public async Task<IActionResult> RemoveFromFavourites(Guid movieId)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return NotFound("Movie does not exist");

            var userId = User.GetUserId();

            if (!await FavouriteRepository.MovieIsFavourite(tx, movieId, userId))
                return BadRequest("This movie is not on your favourite list");

            await FavouriteRepository.UnsetMovieAsFavourite(tx, userId, movieId);
            return NoContent();
        });
    }
}