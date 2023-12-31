﻿using Microsoft.AspNetCore.Mvc;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatchlistController(IWatchlistRepository watchlistRepository) : ControllerBase
{
    private IWatchlistRepository WatchlistRepository { get; } = watchlistRepository;
    
    [HttpGet]
    public async Task<IActionResult> GetAllMoviesOnWatchlist()
    {
        var userId = User.GetUserId();
        var movies = await WatchlistRepository.GetAllMoviesOnWatchlist(userId);

        return Ok(movies);
    }
    
    [HttpPost("{movieId:int}")]
    public async Task<IActionResult> AddToWatchList(int movieId)
    {
        var userId = User.GetUserId();
        var result = await WatchlistRepository.AddToWatchList(userId, movieId);

        return result switch
        {
            QueryResult.Completed => Ok(),
            QueryResult.NotFound => NotFound("Movie not found"),
            QueryResult.EntityAlreadyExists => BadRequest("Movie already in watchlist"),
            _ => throw new Exception(nameof(result))
        };
    }
    
    [HttpDelete("{movieId:int}")]
    public async Task<IActionResult> RemoveFromWatchList(int movieId)
    {
        var userId = User.GetUserId();
        var result = await WatchlistRepository.RemoveFromWatchList(userId, movieId);

        return result switch
        {
            QueryResult.Completed => NoContent(),
            QueryResult.NotFound => NotFound("Movie or watchlist not found"),
            _ => throw new Exception(nameof(result))
        };
    }
}