using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class ReviewRepository(IMovieRepository movieRepository, IDriver driver) : Repository(driver), IReviewRepository
{
    private IMovieRepository MovieRepository { get; } = movieRepository;
    
    public async Task<QueryResult> AddReview(Guid userId, UpsertReviewDto reviewDto)
    {
        return await ExecuteAsync(async tx =>
        {
            if (!await MovieRepository.MovieExists(tx, reviewDto.MovieId))
                return QueryResult.NotFound;
            
            // language=Cypher
            const string relationQuery = """
                                         MATCH (:User { Id: $userId})-[r:REVIEWED]->(:Movie { Id: $movieId })
                                         RETURN r
                                         """;

            var relationResult = await tx.RunAsync(relationQuery,
                new { userId = userId.ToString(), movieId = reviewDto.MovieId.ToString() });

            try
            {
                await relationResult.SingleAsync();
                return QueryResult.EntityAlreadyExists;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
            
            // language=Cypher
            const string query = """
                                 MATCH (u:User { Id: $userId}), (m:Movie { Id: $movieId })
                                 CREATE (u)-[r:REVIEWED {score: $score}]->(m)
                                 RETURN r
                                 """;

            var reviewResult =
                await tx.RunAsync(query,
                    new { userId = userId.ToString(), movieId = reviewDto.MovieId.ToString(), score = reviewDto.Score });

            try
            {
                await reviewResult.SingleAsync();
                return QueryResult.Completed;
            }
            catch (InvalidOperationException)
            {
                return QueryResult.UnexpectedError;
            }
        });
    }

    private static async Task<bool> ReviewExists(IAsyncQueryRunner tx, Guid movieId, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH(:User { Id: $userId })-[r:REVIEWED]->(:Movie { Id: $movieId })
                             WITH COUNT(r) > 0 AS reviewExists
                             RETURN reviewExists
                             """;
        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), movieId = movieId.ToString() });
        return await cursor.SingleAsync(record => record["reviewExists"].As<bool>());
    }
}
