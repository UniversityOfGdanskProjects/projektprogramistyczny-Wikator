using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class WatchlistRepository(IDriver driver) : Repository(driver), IWatchlistRepository
{
    public async Task<IEnumerable<MovieDto>> GetAllMoviesOnWatchlist(int userId)
    {
        return await ExecuteAsync(async tx =>
        {
            var query = $$"""
                          MATCH (m:Movie)<-[:{{RelationshipType.WATCHLIST}}]-(u:User)
                          WHERE Id(u) = $userId
                          OPTIONAL MATCH (m)<-[:{{RelationshipType.PLAYED_IN}}]-(a:Actor)
                          OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
                          WITH m, COLLECT(
                            CASE
                              WHEN a IS NULL THEN null
                              ELSE {
                                Id: ID(a),
                                FirstName: a.FirstName,
                                LastName: a.LastName,
                                DateOfBirth: a.DateOfBirth,
                                Biography: a.Biography
                              }
                            END
                          ) AS Actors, AVG(r.score) AS AverageReviewScore
                          RETURN {
                            Id: ID(m),
                            Title: m.Title,
                            Description: m.Description,
                            Actors: Actors,
                            AverageReviewScore: COALESCE(AverageReviewScore, 0)
                          } AS MovieWithActors
                          """;

            var result = await tx.RunAsync(query, new { userId });
            return await result.ToListAsync(record =>
            {
                var movieWithActorsDto = record["MovieWithActors"].As<IDictionary<string, object>>();
                var actors = movieWithActorsDto["Actors"].As<List<IDictionary<string, object>>>();

                return new MovieDto(
                    movieWithActorsDto["Id"].As<int>(),
                    movieWithActorsDto["Title"].As<string>(),
                    movieWithActorsDto["Description"].As<string>(),
                    movieWithActorsDto["AverageReviewScore"].As<double>(),
                    actors.Select(MapActorDto)
                );
            });
        });
    }

    public async Task<QueryResult> AddToWatchList(int userId, int movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            const string checkMovieExistsQuery = """
                                                 MATCH (m:Movie)
                                                 WHERE Id(m) = $movieId
                                                 RETURN m
                                                 """;

            var movieExistsResult = await tx.RunAsync(checkMovieExistsQuery, new { movieId });

            try
            {
                await movieExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }

            var checkIfWatchlistExists = $"""
                                          MATCH (u:User)-[r:{RelationshipType.WATCHLIST}]->(m:Movie)
                                          WHERE Id(u) = $userId AND Id(m) = $movieId
                                          RETURN r
                                          """;

            var watchlistExistsResult = await tx.RunAsync(checkIfWatchlistExists, new { userId, movieId });

            try
            {
                await watchlistExistsResult.SingleAsync();
                return QueryResult.EntityAlreadyExists;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
            
            var createNewQuery = $"""
                                  MATCH (u:User), (m:Movie)
                                  WHERE Id(u) = $userId AND Id(m) = $movieId
                                  CREATE (u)-[r:{RelationshipType.WATCHLIST}]->(m)
                                  """;
            
            await tx.RunAsync(createNewQuery, new { userId, movieId });
            return QueryResult.Completed;
        });
    }

    public async Task<QueryResult> RemoveFromWatchList(int userId, int movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            const string checkMovieExistsQuery = """
                                                 MATCH (m:Movie)
                                                 WHERE Id(m) = $movieId
                                                 RETURN m
                                                 """;

            var movieExistsResult = await tx.RunAsync(checkMovieExistsQuery, new { movieId });

            try
            {
                await movieExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }

            const string checkIfWatchlistExists = """
                                                  MATCH (u:User)-[r:WATCHLIST]->(m:Movie)
                                                  WHERE Id(u) = $userId AND Id(m) = $movieId
                                                  RETURN r
                                                  """;

            var watchlistExistsResult = await tx.RunAsync(checkIfWatchlistExists, new { userId, movieId });

            try
            {
                await watchlistExistsResult.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                return QueryResult.NotFound;
            }

            const string removeFromWatchlistQuery = """
                                                    MATCH (u:User)-[r:WATCHLIST]->(m:Movie)
                                                    WHERE Id(u) = $userId AND Id(m) = $movieId
                                                    DELETE r
                                                    """;

            await tx.RunAsync(removeFromWatchlistQuery, new { userId, movieId });
            return QueryResult.Completed;
        });
    }
    
    private static ActorDto MapActorDto(IDictionary<string, object> actorData)
    {
        return new ActorDto(
            actorData["Id"].As<int>(),
            actorData["FirstName"].As<string>(),
            actorData["LastName"].As<string>(),
            actorData["DateOfBirth"].As<string>(),
            actorData["Biography"].As<string>()
        );
    }
}