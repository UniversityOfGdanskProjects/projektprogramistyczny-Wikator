using Microsoft.AspNetCore.Mvc;
using MoviesApi.Helpers;
using Neo4j.Driver;

namespace MoviesApi.Controllers.Base;

[ApiController]
[ServiceFilter(typeof(LogUserActivity))]
public abstract class BaseApiController(IDriver driver) : ControllerBase
{
    private IDriver Driver { get; } = driver;
    
    protected async Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteReadAsync(query);
    }
    
    protected async Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteWriteAsync(query);
    }
}
