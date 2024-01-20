using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class GenreRepository : IGenreRepository
{
    public async Task<List<string>> GetAllGenres(IAsyncQueryRunner tx)
    {
        // language=Cypher
        const string query = "MATCH (g:Genre) RETURN g.name AS name ORDER BY name ASC";
        var result = await tx.RunAsync(query);
        return await result.ToListAsync(record => record["name"].As<string>());
    }
}