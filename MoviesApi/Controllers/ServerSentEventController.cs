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
    public async Task Get(CancellationToken cancellationToken, [FromQuery] int interval = 1200)
    {
        var response = Response;
        response.Headers.Append("Content-Type", "text/event-stream");

        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                await using var session = Driver.AsyncSession();
                var movieTitle = await session.ExecuteReadAsync(async tx => await MovieRepository.GetMostPopularMovieTitle(tx));
                await response.WriteAsync($"data: {movieTitle}\r\r", cancellationToken: cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
                await Task.Delay(interval * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
