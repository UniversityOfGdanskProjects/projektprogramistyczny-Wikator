using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class WatchlistRepository(IMovieRepository movieRepository, IDriver driver) : Repository(driver), IWatchlistRepository
{
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
    public async Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(Guid userId)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie)<-[:WATCHLIST]-(u:User {Id: $userId})
                                 OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                                 OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
                                 OPTIONAl MATCH (m)<-[c:COMMENTED]-(u2:User)
                                 WITH m, COLLECT(
                                   CASE
                                     WHEN a IS NULL THEN null
                                     ELSE {
                                       Id: a.Id,
                                       FirstName: a.FirstName,
                                       LastName: a.LastName,
                                       DateOfBirth: a.DateOfBirth,
                                       Biography: a.Biography,
                                       PictureAbsoluteUri: a.PictureAbsoluteUri
                                     }
                                   END
                                 ) AS Actors,
                                 COLLECT(
                                   CASE
                                     WHEN u2 is NULL OR c is NULL THEN null
                                     ELSE {
                                       Id: c.Id,
                                       MovieId: m.Id,
                                       UserId: u2.Id,
                                       Username: u2.Name,
                                       Text: c.Text,
                                       CreatedAt: c.CreatedAt,
                                       IsEdited: c.IsEdited
                                     }
                                   END
                                 ) AS Comments, AVG(r.score) AS AverageReviewScore
                                 RETURN {
                                   Id: m.Id,
                                   Title: m.Title,
                                   Description: m.Description,
                                   InTheaters: m.InTheaters,
                                   TrailerAbsoluteUri: m.TrailerAbsoluteUri,
                                   PictureAbsoluteUri: m.PictureAbsoluteUri,
                                   ReleaseDate: m.ReleaseDate,
                                   MinimumAge: m.MinimumAge,
                                   Actors: Actors,
                                   Comments: Comments,
                                   AverageReviewScore: COALESCE(AverageReviewScore, 0)
                                 } AS MovieWithActors
                                 """;

            var result = await tx.RunAsync(query, new { userId = userId.ToString() });
            return await result.ToListAsync(record =>
            {
                var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
                return movieWithActorsDto.ConvertToMovieDto();
            });
        });
    }

    public async Task<QueryResult> AddToWatchList(Guid userId, Guid movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return QueryResult.NotFound;

            if (!await WishlistExists(tx, movieId, userId))
                return QueryResult.EntityAlreadyExists;
            
            // language=Cypher
            const string createNewQuery = """
                                          MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                                          CREATE (u)-[r:WATCHLIST]->(m)
                                          """;
            
            await tx.RunAsync(createNewQuery, new { userId = userId.ToString(), movieId = movieId.ToString() });
            return QueryResult.Completed;
        });
    }

    public async Task<QueryResult> RemoveFromWatchList(Guid userId, Guid movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return QueryResult.NotFound;

            if (! await WishlistExists(tx, movieId, userId))
                return QueryResult.NotFound;

            // language=Cypher
            const string removeFromWatchlistQuery = """
                                                    MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                                                    DELETE r
                                                    """;

            await tx.RunAsync(removeFromWatchlistQuery,
                new { userId = userId.ToString(), movieId = movieId.ToString() });
            return QueryResult.Completed;
        });
    }
    
    private static async Task<bool> WishlistExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
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