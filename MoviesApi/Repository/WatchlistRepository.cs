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
                             MATCH (m:Movie)<-[:WATCHLIST]-(u:User { Id: $userId })
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             OPTIONAL MATCH (m)<-[f:FAVOURITE]-(u)
                             OPTIONAL MATCH (m)<-[ur:REVIEWED]-(u)
                             WITH m, AVG(r.Score) AS AverageReviewScore, f IS NOT NULL AS IsFavourite, COUNT(r) AS ReviewsCount, ur.Score AS UserReviewScore
                             RETURN {
                               Id: m.Id,
                               Title: m.Title,
                               PictureAbsoluteUri: m.PictureAbsoluteUri,
                               MinimumAge: m.MinimumAge,
                               OnWatchlist: true,
                               IsFavourite: IsFavourite,
                               UserReviewScore: UserReviewScore,
                               ReviewsCount: ReviewsCount,
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

    public async Task AddToWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                             CREATE (u)-[r:WATCHLIST]->(m)
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task RemoveFromWatchList(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
    }
    
    public async Task<bool> WatchlistExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string checkIfWatchlistExistsQuery = """
                                                   MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                                                   WITH COUNT(r) > 0 AS watchlistExists
                                                   RETURN watchlistExists
                                                   """;
        
        var watchlistExistsCursor = await tx.RunAsync(checkIfWatchlistExistsQuery,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await watchlistExistsCursor.SingleAsync(record => record["watchlistExists"].As<bool>());
    }
}