using Neo4j.Driver;

namespace MoviesApi.Repository;

public abstract class Repository(IDriver driver)
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
