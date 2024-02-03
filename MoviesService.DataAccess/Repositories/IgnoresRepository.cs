using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class IgnoresRepository : IIgnoresRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[:IGNORES]-(u:User { id: $userId })
                             OPTIONAL MATCH (g:Genre)<-[:IS]-(m)
                             WITH m, u,
                               COLLECT(
                                 CASE
                                   WHEN g IS NOT NULL THEN g.name
                                 END
                               ) AS genres
                             OPTIONAL MATCH (:User)-[r:REVIEWED]->(m)
                             WITH m, u, genres, COUNT(r) AS reviewsCount, COALESCE(AVG(r.score), 0) AS averageReviewScore
                             OPTIONAL MATCH (u)-[r:REVIEWED]->(m)
                             WITH m, u, genres, reviewsCount, averageReviewScore, CASE WHEN r IS NOT NULL THEN { id: r.id, score: r.score } END AS userReviewScore
                             RETURN
                               m.id AS id,
                               m.title AS title,
                               m.pictureAbsoluteUri AS pictureAbsoluteUri,
                               m.minimumAge AS minimumAge,
                               EXISTS { MATCH (u)-[:WATCHLIST]->(m) } AS onWatchlist,
                               EXISTS { MATCH (u)-[:FAVOURITE]->(m) } AS isFavourite,
                               userReviewScore,
                               reviewsCount,
                               COALESCE(genres, []) AS genres,
                               averageReviewScore
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record => record.ConvertToMovieDto());
    }

    public async Task IgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:IGNORES]->(m)
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task RemoveIgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:IGNORES]->(:Movie { id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task<bool> IgnoresExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:IGNORES]->(:Movie { id: $movieId })
                             RETURN COUNT(r) > 0 AS isIgnored
                             """;

        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["isIgnored"].As<bool>());
    }
}