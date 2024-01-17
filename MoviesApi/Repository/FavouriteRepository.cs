using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class FavouriteRepository : IFavouriteRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllFavouriteMovies(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (m:Movie)<-[:FAVOURITE]-(u:User { id: $userId })
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             OPTIONAL MATCH (m)<-[w:WATCHLIST]-(u)
                             OPTIONAL MATCH (m)<-[ur:REVIEWED]-(u)
                             WITH m, COALESCE(AVG(r.score), 0) AS averageReviewScore, w IS NOT NULL AS onWatchlist, COUNT(r) AS reviewsCount, ur.score AS userReviewScore
                             RETURN
                               m.id AS id,
                               m.title AS title,
                               m.pictureAbsoluteUri AS pictureAbsoluteUri,
                               m.minimumAge AS minimumAge,
                               onWatchlist,
                               true AS isFavourite,
                               userReviewScore,
                               reviewsCount,
                               averageReviewScore
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record => record.ConvertToMovieDto());
    }

    public async Task SetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task UnsetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:FAVOURITE]->(:Movie { id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }
    
    public async Task<bool> MovieIsFavourite(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:FAVOURITE]->(:Movie { id: $movieId })
                             RETURN COUNT(r) > 0 AS favouriteExists
                             """;
        
        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["favouriteExists"].As<bool>());
    }
}