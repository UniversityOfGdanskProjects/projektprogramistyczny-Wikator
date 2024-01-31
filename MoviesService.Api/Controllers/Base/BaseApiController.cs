using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace MoviesService.Api.Controllers.Base;

[ApiController]
public abstract class BaseApiController(IDriver driver) : ControllerBase
{
    protected IDriver Driver { get; } = driver;
    
    protected async Task<IActionResult> ExecuteReadAsync(Func<IAsyncQueryRunner, Task<IActionResult>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteReadAsync(query);
    }
    
    protected async Task<IActionResult> ExecuteWriteAsync(Func<IAsyncQueryRunner, Task<IActionResult>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteWriteAsync(query);
    }
}
