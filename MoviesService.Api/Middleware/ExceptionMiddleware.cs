using System.Net;
using MoviesService.Api.Exceptions;
using Neo4j.Driver;

namespace MoviesService.Api.Middleware;

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
                case PhotoServiceException photoServiceException:
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    await context.Response.WriteAsync(photoServiceException.Message);
                    break;

                case ServiceUnavailableException:
                    context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    await context.Response.WriteAsync(
                        "Access to database is currently unavailable, try again in few minutes.");
                    break;

                case InvalidOperationException or Neo4jException:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync(
                        "There was an error when accessing database, please contact the administrator.");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await context.Response.WriteAsync("An unexpected error occurred, please try again later.");
                    break;
            }
        }
    }
}