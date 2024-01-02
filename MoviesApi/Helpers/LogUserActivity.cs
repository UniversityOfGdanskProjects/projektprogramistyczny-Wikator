using Microsoft.AspNetCore.Mvc.Filters;
using MoviesApi.Extensions;
using Neo4j.Driver;

namespace MoviesApi.Helpers;

public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();
        
        if (resultContext.HttpContext.User.Identity is not { IsAuthenticated: true })
            return;
        
        var userId = resultContext.HttpContext.User.GetUserId();
        var driver = resultContext.HttpContext.RequestServices.GetRequiredService<IDriver>();
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId })
                             SET u.LastActive = datetime()
                             """;
        
        await using var session = driver.AsyncSession();
        await session.ExecuteWriteAsync(tx => tx.RunAsync(query,
            new { userId = userId.ToString()}));
    }
}