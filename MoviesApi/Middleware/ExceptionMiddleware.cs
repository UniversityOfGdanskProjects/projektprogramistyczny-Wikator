using System.Net;
using Neo4j.Driver;

namespace MoviesApi.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private RequestDelegate Next { get; } = next;
    private ILogger<ExceptionMiddleware> Logger { get; } = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, exception.Message);
            context.Response.ContentType = "application/json";

            switch (exception)
            {
                case InvalidOperationException or Neo4jException or ServiceUnavailableException:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("There was an error when accessing database, please try again later.");
                    break;
                
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("An unexpected error occurred, please try again later.");
                    break;
            }
        }
    }
}