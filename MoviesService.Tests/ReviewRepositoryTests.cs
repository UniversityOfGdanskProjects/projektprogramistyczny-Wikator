using MoviesService.Core.Extensions;
using MoviesService.DataAccess.Repositories;
using MoviesService.Models.DTOs.Requests;
using Neo4j.Driver;

namespace MoviesService.Tests;

[Collection("DatabaseCollection")]
public class ReviewRepositoryTests
{
    private TestDatabaseSetup Database { get; }
    private Guid ReviewId { get; } = Guid.NewGuid();

    public ReviewRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:REVIEWED { id: $reviewId, score: 5 }]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString(),
            reviewId = ReviewId.ToString()
        };
        using var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters)).Wait();
    }
    
    [Theory]
    [InlineData(4)]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(1)]
    public async Task AddReview_ShouldReturnReviewDto(int score)
    {
        // Arrange
        var repository = new ReviewRepository();
        var dto = new AddReviewDto { MovieId = Database.MovieId, Score = score };
        await using var session = Database.Driver.AsyncSession();

        // Act
        var reviewDto = await session.ExecuteWriteAsync(async tx =>
        {
            await repository.AddReview(tx, Database.UserId, dto);
            
            // language=Cypher
            const string query = """
                                 MATCH (u:User { id: $userId})-[r:REVIEWED { score: $score }]->(m:Movie { id: $movieId})
                                 RETURN
                                   r.id AS id,
                                   u.id AS userId,
                                   m.id AS movieId,
                                   r.score AS score
                                 """;
            
            var parameters = new { userId = Database.UserId.ToString(), movieId = Database.MovieId.ToString(), score };
            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => record.ConvertToReviewDto());
        });
        
        // Assert
        Assert.Equal(Database.MovieId, reviewDto.MovieId);
        Assert.Equal(dto.Score, reviewDto.Score);
        Assert.Equal(Database.UserId, reviewDto.UserId);
    }
    
    [Fact]
    public async Task UpdateReview_ShouldUpdateReviewDto()
    {
        // Arrange
        var repository = new ReviewRepository();
        const int updatedScore = 4;
        await using var session = Database.Driver.AsyncSession();

        // Act
        var updatedReviewDto = await session.ExecuteWriteAsync(async tx =>
        {
            await repository.UpdateReview(tx, Database.UserId, ReviewId, new UpdateReviewDto { Score = updatedScore });

            // language=Cypher
            const string query = """
                                 MATCH (u:User { id: $userId })-[r:REVIEWED { id: $reviewId }]->(m:Movie { id: $movieId })
                                 RETURN
                                     r.id AS id,
                                     u.id AS userId,
                                     m.id AS movieId,
                                     r.score AS score
                                 """;

            var parameters = new { userId = Database.UserId.ToString(), movieId = Database.MovieId.ToString(), reviewId = ReviewId.ToString() };
            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => record.ConvertToReviewDto());
        });

        // Assert
        Assert.NotNull(updatedReviewDto);
        Assert.Equal(Database.MovieId, updatedReviewDto.MovieId);
        Assert.Equal(updatedScore, updatedReviewDto.Score);
        Assert.Equal(Database.UserId, updatedReviewDto.UserId);
    }

    [Fact]
    public async Task ReviewExists_ShouldReturnTrueIfReviewExists()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var reviewExists = await session.ExecuteWriteAsync(async tx => await repository.ReviewExists(tx, ReviewId, Database.UserId));
        
        // Assert
        Assert.True(reviewExists);
    }
    
    [Fact]
    public async Task ReviewExists_ShouldFalseTrueIfReviewDoesNotExist()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var reviewExists = await session.ExecuteWriteAsync(async tx => await repository.ReviewExists(tx, Guid.NewGuid(), Database.UserId));
        
        // Assert
        Assert.False(reviewExists);
    }

    [Fact]
    public async Task DeleteReview_ShouldDeleteReview()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
    
        // Act
        await session.ExecuteWriteAsync(async tx => await repository.DeleteReview(tx, Database.UserId, ReviewId));
    
        // Assert
        var reviewExists = await repository.ReviewExists(session, ReviewId, Database.UserId);
        Assert.False(reviewExists);
    }
    
    [Fact]
    public async Task ReviewExistsByMovieId_ShouldReturnTrueIfReviewExists()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var reviewExists = await session.ExecuteWriteAsync(async tx => await repository.ReviewExistsByMovieId(tx, Database.MovieId, Database.UserId));
        
        // Assert
        Assert.True(reviewExists);
    }
    
    [Fact]
    public async Task ReviewExistsByMovieId_ShouldReturnTrueIfReviewDoesNotExist()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var reviewExists = await session.ExecuteWriteAsync(async tx => await repository.ReviewExistsByMovieId(tx, Guid.NewGuid(), Database.UserId));
        
        // Assert
        Assert.False(reviewExists);
    }
}