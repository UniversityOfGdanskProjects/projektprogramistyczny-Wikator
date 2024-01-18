using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class WatchlistRepository : IWatchlistRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[:WATCHLIST]-(u:User { id: $userId })
                             OPTIONAL MATCH (g:Genre)<-[:IS]-(m)
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             OPTIONAL MATCH (m)<-[f:FAVOURITE]-(u)
                             OPTIONAL MATCH (m)<-[ur:REVIEWED]-(u)
                             WITH m,
                               COLLECT(
                                    CASE
                                    WHEN g IS NOT NULL THEN g.name
                                    END
                                ) AS genres, COALESCE(AVG(r.score), 0) AS averageReviewScore, f IS NOT NULL AS isFavourite, COUNT(r) AS reviewsCount, ur.score AS userReviewScore
                             RETURN
                               m.id AS id,
                               m.title AS title,
                               m.pictureAbsoluteUri AS pictureAbsoluteUri,
                               m.minimumAge AS minimumAge,
                               true AS onWatchlist,
                               isFavourite,
                               userReviewScore,
                               reviewsCount,
                               COALESCE(genres, []) AS genres,
                               averageReviewScore
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record => record.ConvertToMovieDto());
    }

    public async Task AddToWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:WATCHLIST]->(m)
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task RemoveFromWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:WATCHLIST]->(:Movie { id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }
    
    public async Task<bool> WatchlistExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:WATCHLIST]->(:Movie { id: $movieId })
                             RETURN COUNT(r) > 0 AS watchlistExists
                             """;
        
        var parameters = new
        {
            userId = userId.ToString(),
            movieId = movieId.ToString()
        };
        
        var watchlistExistsCursor = await tx.RunAsync(query, parameters);
        return await watchlistExistsCursor.SingleAsync(record => record["watchlistExists"].As<bool>());
    }
}
