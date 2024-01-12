using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class IgnoresRepository : IIgnoresRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[:IGNORES]-(u:User { Id: $userId })
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             OPTIONAL MATCH (m)<-[w:WATCHLIST]-(u)
                             OPTIONAL MATCH (m)<-[f:FAVOURITE]-(u)
                             WITH m, AVG(r.Score) AS AverageReviewScore, w IS NOT NULL AS OnWatchlist, f IS NOT NULL AS IsFavourite
                             RETURN {
                               Id: m.Id,
                               Title: m.Title,
                               PictureAbsoluteUri: m.PictureAbsoluteUri,
                               MinimumAge: m.MinimumAge,
                               OnWatchlist: OnWatchlist,
                               IsFavourite: IsFavourite,
                               AverageReviewScore: COALESCE(AverageReviewScore, 0)
                             } AS MovieWithActors
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record =>
        {
            var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
            return movieWithActorsDto.ConvertToMovieDto();
        });
    }

    public async Task IgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                             CREATE (u)-[r:IGNORES]->(m)
                             """;
        
        await tx.RunAsync(query,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task RemoveIgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string removeMovieFromIgnoredQuery = """
                                                   MATCH (:User { Id: $userId })-[r:IGNORES]->(:Movie { Id: $movieId })
                                                   DELETE r
                                                   """;

        await tx.RunAsync(removeMovieFromIgnoredQuery,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task<bool> IgnoresExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:IGNORES]->(:Movie { Id: $movieId })
                             WITH COUNT(r) > 0 AS IsIgnored
                             RETURN IsIgnored
                             """;

        var cursor = await tx.RunAsync(query,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["IsIgnored"].As<bool>());
    }
}
