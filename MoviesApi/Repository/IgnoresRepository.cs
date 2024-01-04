using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Extensions;
using MoviesApi.Helpers;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class IgnoresRepository(IMovieRepository movieRepository, IDriver driver) : Repository(driver), IIgnoresRepository
{
    private IMovieRepository MovieRepository { get; } = movieRepository;

    public async Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(Guid userId)
    {
        return await ExecuteAsync(async tx =>
        {
            // language=Cypher
            const string query = """
                                 MATCH (m:Movie)<-[:WATCHLIST]-(u:User { Id: $userId })
                                 OPTIONAL MATCH (m)<-[:IGNORES]-(a:Actor)
                                 OPTIONAL MATCH (m)<-[r:REVIEWED]-(u:User)
                                 WITH m, COLLECT(
                                   CASE
                                     WHEN a IS NULL THEN null
                                     ELSE {
                                       Id: a.Id,
                                       FirstName: a.FirstName,
                                       LastName: a.LastName,
                                       DateOfBirth: a.DateOfBirth,
                                       Biography: a.Biography
                                     }
                                   END
                                 ) AS Actors, AVG(r.score) AS AverageReviewScore
                                 RETURN {
                                   Id: m.Id,
                                   Title: m.Title,
                                   Description: m.Description,
                                   Actors: Actors,
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

    public async Task<QueryResult> IgnoreMovie(Guid userId, Guid movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return new QueryResult(QueryResultStatus.NotFound);
            
            if (await IgnoresExists(tx, movieId, userId))
                return new QueryResult(QueryResultStatus.EntityAlreadyExists);
            
            // language=Cypher
            const string createNewQuery = """
                                          MATCH (u:User { Id: $userId }), (m:Movie { Id: $movieId })
                                          CREATE (u)-[r:IGNORES]->(m)
                                          """;
            
            await tx.RunAsync(createNewQuery,
                new { userId = userId.ToString(), movieId = movieId.ToString() });
            
            return new QueryResult(QueryResultStatus.Completed);
        });
    }

    public async Task<QueryResult> RemoveIgnoreMovie(Guid userId, Guid movieId)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, movieId))
                return new QueryResult(QueryResultStatus.NotFound);
            
            if (await IgnoresExists(tx, movieId, userId))
                return new QueryResult(QueryResultStatus.RelationDoesNotExist);
            
            // language=Cypher
            const string removeMovieFromIgnoredQuery = """
                                                       MATCH (:User { Id: $userId })-[r:IGNORES]->(:Movie { Id: $movieId })
                                                       DELETE r
                                                       """;

            await tx.RunAsync(removeMovieFromIgnoredQuery,
                new { userId = userId.ToString(), movieId = movieId.ToString() });

            return new QueryResult(QueryResultStatus.Completed);
        });
    }

    private static async Task<bool> IgnoresExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
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
