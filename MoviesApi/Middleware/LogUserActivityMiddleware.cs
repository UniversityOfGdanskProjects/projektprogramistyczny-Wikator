using System.Security.Claims;
using Neo4j.Driver;

namespace MoviesApi.Middleware;

public class LogUserActivityMiddleware(RequestDelegate next)
{
    private RequestDelegate Next { get; } = next;
    
    public async Task InvokeAsync(HttpContext context, IDriver driver)
    {
        await Next(context);

        var userId = context.User.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (userId is null)
            return;

        _ = LogUserActivityInBackground(driver, Guid.Parse(userId));
    }

    private static async Task LogUserActivityInBackground(IDriver driver, Guid userId)
    {
        // language=Cypher
        const string query =  """
                              MATCH (u:User { id: $userId })
                              SET u.lastActive = datetime(), u.activityScore = u.activityScore + 1
                              """;
        
        await using var session = driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(query, new { userId = userId.ToString() });
        });
    }
}
