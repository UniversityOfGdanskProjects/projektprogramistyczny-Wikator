using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class WatchlistRepository(IDriver driver) : Repository(driver), IWatchlistRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(Guid userId)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie)<-[:WATCHLIST]-(u:User {Id: $userId})
                                 OPTIONAL MATCH (m)<-[:PLAYED_IN]-(a:Actor)
                                 OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
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
                                 ) AS Actors
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
                                   AverageReviewScore: 0
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
            // language=Cypher
            const string checkMovieExistsQuery = """
                                                 MATCH (m:Movie {Id: $movieId})
                                                 RETURN m
                                                 """;

            var movieExistsResult = await tx.RunAsync(checkMovieExistsQuery, new { movieId = movieId.ToString() });

            try
            {
                await movieExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }

            // language=Cypher
            const string checkIfWatchlistExists = """
                                                  MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                                                  RETURN r
                                                  """;

            var watchlistExistsResult = await tx.RunAsync(checkIfWatchlistExists,
                new { userId = userId.ToString(), movieId = movieId.ToString() });

            try
            {
                await watchlistExistsResult.SingleAsync();
                return QueryResult.EntityAlreadyExists;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
            
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
            // language=Cypher
            const string checkMovieExistsQuery = """
                                                 MATCH (m:Movie { Id: $movieId })
                                                 RETURN m
                                                 """;

            var movieExistsResult = await tx.RunAsync(checkMovieExistsQuery, new { movieId = movieId.ToString() });

            try
            {
                await movieExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }
            
            // language=Cypher
            const string checkIfWatchlistExists = """
                                                  MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                                                  RETURN r
                                                  """;

            var watchlistExistsResult = await tx.RunAsync(checkIfWatchlistExists,
                new { userId = userId.ToString(), movieId = movieId.ToString() });

            try
            {
                await watchlistExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }
            
            // language=Cypher
            const string removeFromWatchlistQuery = """
                                                    MATCH (:User { Id: $userId })-[r:WATCHLIST]->(:Movie { Id: $movieId })
                                                    DELETE r
                                                    """;

            await tx.RunAsync(removeFromWatchlistQuery, new { userId = userId.ToString(), movieId = movieId.ToString() });
            return QueryResult.Completed;
        });
    }
}