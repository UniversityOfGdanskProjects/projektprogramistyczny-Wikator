﻿using System.Security.Claims;
using Neo4j.Driver;

namespace MoviesApi.Middleware;

public class LogUserActivityMiddleware(RequestDelegate next)
{
    private RequestDelegate Next { get; } = next;
    
    public async Task InvokeAsync(HttpContext context, IDriver driver, ILogger<LogUserActivityMiddleware> logger)
    {
        await Next(context);

        var userId = context.User.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        
        if (userId is null)
            return;

        _ = LogUserActivityInBackground(driver, Guid.Parse(userId), logger);
    }

    private static async Task LogUserActivityInBackground(IDriver driver, Guid userId, ILogger<LogUserActivityMiddleware> logger)
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
        
        logger.LogInformation($"User with id {userId} has been updated.");
    }
}
