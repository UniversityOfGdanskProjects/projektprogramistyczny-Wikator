using System.Security.Claims;
using MoviesService.Api.Services.Contracts;
using Neo4j.Driver;

namespace MoviesService.Api.Middleware;

public class LogUserActivityMiddleware(RequestDelegate next)
{
    private RequestDelegate Next { get; } = next;

    public async Task InvokeAsync(HttpContext context, IDriver driver,
        ILogger<LogUserActivityMiddleware> logger, IMqttService mqttService)
    {
        await Next(context);

        var userId = context.User.Claims
            .FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
            return;

        await LogUserActivityInBackground(driver, Guid.Parse(userId), logger, mqttService);
    }

    private static async Task LogUserActivityInBackground(IDriver driver, Guid userId,
        ILogger<LogUserActivityMiddleware> logger, IMqttService mqttService)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             WITH date(u.lastActive) AS previousDate, u
                             SET u.lastActive = datetime(), u.activityScore = u.activityScore + 1
                             RETURN previousDate <> date() AS isNewDay
                             """;

        await using var session = driver.AsyncSession();
        var isNewDay = await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { userId = userId.ToString() });
            return await cursor.SingleAsync(record => record["isNewDay"].As<bool>());
        });

        if (isNewDay) await mqttService.SendNotificationAsync("users/new-today", "New user today!");

        logger.LogInformation($"User with id {userId} has been updated.");
    }
}