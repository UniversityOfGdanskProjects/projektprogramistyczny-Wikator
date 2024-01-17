using Microsoft.AspNetCore.Mvc.Filters;
using MoviesApi.Extensions;
using Neo4j.Driver;

namespace MoviesApi.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (!resultContext.HttpContext.User.Identity?.IsAuthenticated ?? false)
                return;

            var driver = resultContext.HttpContext.RequestServices.GetRequiredService<IDriver>();
            var userId = resultContext.HttpContext.User.GetUserId();
            
            _ = LogUserActivityInBackground(driver, userId);
        }

        private static async Task LogUserActivityInBackground(IDriver driver, Guid userId)
        {
            // language=Cypher
            const string query =  """
                                  MATCH (u:User { id: $userId })
                                  SET u.lastActive = datetime()
                                  """;
            
            await using var session = driver.AsyncSession();
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, new { userId = userId.ToString() });
            });
        }
    }
}