using Neo4j.Driver;

namespace MoviesService.DataAccess.Contracts;

public interface IAsyncQueryExecutor
{
    Task<T> ExecuteReadAsync<T>(Func<IAsyncQueryRunner, Task<T>> query);
    Task<T> ExecuteWriteAsync<T>(Func<IAsyncQueryRunner, Task<T>> query);
}