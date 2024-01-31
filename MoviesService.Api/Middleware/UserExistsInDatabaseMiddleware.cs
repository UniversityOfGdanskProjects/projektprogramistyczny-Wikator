using System.Net;
using System.Security.Claims;
using Neo4j.Driver;

namespace MoviesService.Api.Middleware;

public class UserExistsInDatabaseMiddleware(RequestDelegate next)
{
    private RequestDelegate Next { get; } = next;

    public async Task InvokeAsync(HttpContext context, IDriver driver)
    {
        var userId = context.User.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (userId is not null)
        {
            await using var sessions = driver.AsyncSession();
            var result = await sessions.ExecuteReadAsync(async transaction =>
            {
                // language=Cypher
                var cursor = await transaction.RunAsync(
                    "MATCH (u:User { id: $userId }) RETURN COUNT(u) > 0 AS userExists",
                    new { userId });

                return await cursor.SingleAsync(record => record["userExists"].As<bool>());
            });

            if (!result)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("User no longer exists. Please create a new account.");
                return;
            }
        }

        await Next(context);
    }
}