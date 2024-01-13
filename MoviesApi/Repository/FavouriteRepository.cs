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
                             MATCH (m:Movie)<-[:FAVOURITE]-(u:User { Id: $userId })
                             OPTIONAL MATCH (m)<-[r:REVIEWED]-(:User)
                             OPTIONAL MATCH (m)<-[w:WATCHLIST]-(u)
                             OPTIONAL MATCH (m)<-[ur:REVIEWED]-(u)
                             WITH m, AVG(r.Score) AS AverageReviewScore, w IS NOT NULL AS OnWatchlist, COUNT(r) AS ReviewsCount, ur.Score AS UserReviewScore
                             RETURN {
                               Id: m.Id,
                               Title: m.Title,
                               PictureAbsoluteUri: m.PictureAbsoluteUri,
                               MinimumAge: m.MinimumAge,
                               OnWatchlist: OnWatchlist,
                               IsFavourite: true,
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

    public async Task SetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                             CREATE (u)-[r:FAVOURITE]->(m)
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
    }

    public async Task UnsetMovieAsFavourite(IAsyncQueryRunner tx, Guid userId, Guid movieId)
    {
        // language=Cypher
        const string query = """
                             MATCH (:User { Id: $userId })-[r:FAVOURITE]->(:Movie { Id: $movieId })
                             DELETE r
                             """;

        await tx.RunAsync(query,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
    }
    
    public async Task<bool> MovieIsFavourite(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string checkIfWatchlistExistsQuery = """
                                                   MATCH (:User { Id: $userId })-[r:FAVOURITE]->(:Movie { Id: $movieId })
                                                   WITH COUNT(r) > 0 AS favouriteExists
                                                   RETURN favouriteExists
                                                   """;
        
        var cursor = await tx.RunAsync(checkIfWatchlistExistsQuery,
            new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["favouriteExists"].As<bool>());
    }
}