using Neo4j.Driver;

namespace MoviesApi.Repository;

public abstract class Repository(IDriver driver)
{
    protected IDriver Driver { get; } = driver;
    
    protected async Task<T> ExecuteAsync<T>(Func<IAsyncSession, Task<T>> query)
    {
        await using var session = Driver.AsyncSession();
        
        try
        {
            return await query(session);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
