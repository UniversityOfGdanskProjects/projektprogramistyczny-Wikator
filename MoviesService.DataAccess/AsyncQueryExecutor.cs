using MoviesService.DataAccess.Contracts;
using Neo4j.Driver;

namespace MoviesService.DataAccess;

public class AsyncQueryExecutor(IDriver driver) : IAsyncQueryExecutor
{
    private IDriver Driver { get; } = driver;


    public async Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteReadAsync(query);
    }

    public async Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> query)
    {
        await using var session = Driver.AsyncSession();
        return await session.ExecuteWriteAsync(query);
    }
}
