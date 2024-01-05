using Neo4j.Driver;

namespace MoviesApi.DatabaseSetup;

public class Setup(IDriver driver)
{
    private IDriver Driver { get; } = driver;

    public async Task SetupJobs()
    {
        await using var session = Driver.AsyncSession();
        // language=Cypher
        const string jobExistsQuery = "CALL apoc.periodic.list() YIELD name AS job";

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(jobExistsQuery);
            return await cursor.ToListAsync(record => record["job"].As<string>());
        });
        
        if (result.All(job => job != "decrease popularity"))
        {
            // language=Cypher
            const string createJobQuery = """
                                          CALL apoc.periodic.repeat(
                                              'decrease popularity',
                                              'MATCH (m:Movie) SET m.popularity = m.popularity / 2',
                                              43200000
                                          )
                                          """;
            await session.ExecuteWriteAsync(tx => tx.RunAsync(createJobQuery));
        }
    }
}