using Microsoft.AspNetCore.Mvc;
using MoviesService.Api.Controllers.Base;
using MoviesService.DataAccess.Contracts;
using MoviesService.DataAccess.Repositories.Contracts;

namespace MoviesService.Api.Controllers;

[Route("/api/sse")]
public class ServerSentEventController(
    IAsyncQueryExecutor queryExecutor,
    IMovieRepository movieRepository) : BaseApiController(queryExecutor)
{
    private IMovieRepository MovieRepository { get; } = movieRepository;


    [HttpGet]
    public async Task Get(CancellationToken cancellationToken, [FromQuery] int interval = 1200)
    {
        var response = Response;
        response.Headers.Append("Content-Type", "text/event-stream");

        while (cancellationToken.IsCancellationRequested is false)
            try
            {
                var movieTitle =
                    await QueryExecutor.ExecuteReadAsync(async tx =>
                        await MovieRepository.GetMostPopularMovieTitle(tx));

                await response.WriteAsync($"data: {movieTitle}\r\r", cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
                await Task.Delay(interval * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
    }
}