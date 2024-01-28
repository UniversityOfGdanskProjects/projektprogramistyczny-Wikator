using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("/api/sse")]
public class ServerSentEventController(IDriver driver, IMovieRepository movieRepository) : Controller
{
    private IDriver Driver { get; } = driver;
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
    
    [HttpGet]
    [DoesNotReturn]
    public async Task Get()
    {
        var response = Response;
        response.Headers.Append("Content-Type", "text/event-stream");

        while (true)
        {
            await using var session = Driver.AsyncSession();
            var movieTitle = await session.ExecuteReadAsync(async tx => await MovieRepository.GetMostPopularMovieTitle(tx));
            await response
                .WriteAsync($"data: {movieTitle}\r\r");

            await response.Body.FlushAsync();
            await Task.Delay(5 * 60 * 1000);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}
