using Neo4j.Driver;

namespace MoviesApi.Repository;

public abstract class Repository(IDriver driver)
{
    protected IDriver Driver { get; } = driver;
}