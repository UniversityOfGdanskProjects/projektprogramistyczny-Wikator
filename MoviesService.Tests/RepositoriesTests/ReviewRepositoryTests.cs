using FluentAssertions;
using MoviesService.DataAccess.Extensions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class ReviewRepositoryTests
{
    public ReviewRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[:REVIEWED {id: $reviewId, score: 5}]->(m)
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

    private TestDatabaseSetup Database { get; }
    private Guid ReviewId { get; } = Guid.NewGuid();

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

            var parameters = new
            {
                userId = Database.UserId.ToString(), movieId = Database.MovieId.ToString(),
                reviewId = ReviewId.ToString()
            };
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
        var reviewExists =
            await session.ExecuteWriteAsync(async tx => await repository.ReviewExists(tx, ReviewId, Database.UserId));

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
        var reviewExists = await session.ExecuteWriteAsync(async tx =>
            await repository.ReviewExists(tx, Guid.NewGuid(), Database.UserId));

        // Assert
        Assert.False(reviewExists);
    }

    [Fact]
    public async Task GetMovieIdFromReviewId_ShouldReturnId_IfReviewExists()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await repository.GetMovieIdFromReviewId(tx, ReviewId, Database.UserId));
        
        // Assert
        result.Should().Be(Database.MovieId);
    }
    
    [Fact]
    public async Task GetMovieIdFromReviewId_ShouldNull_IfReviewDoesNotExists()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await repository.GetMovieIdFromReviewId(tx, Guid.NewGuid(), Database.UserId));
        
        // Assert
        result.Should().BeNull();
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
        var reviewExists = await session.ExecuteWriteAsync(async tx =>
            await repository.ReviewExistsByMovieId(tx, Database.MovieId, Database.UserId));

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
        var reviewExists = await session.ExecuteWriteAsync(async tx =>
            await repository.ReviewExistsByMovieId(tx, Guid.NewGuid(), Database.UserId));

        // Assert
        Assert.False(reviewExists);
    }

    [Fact]
    public async Task GetAverageAndCountFromReviewId_ShouldReturnAverageAndCount()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var averageAndCount =
            await session.ExecuteWriteAsync(async tx => await repository.GetAverageAndCountFromReviewId(tx, ReviewId));

        // Assert
        Assert.Equal(1, averageAndCount.Count);
        Assert.Equal(5, averageAndCount.Average);
    }

    [Fact]
    public async Task GetAverageAndCountFromMovieIdShouldReturnAverageAndCount()
    {
        // Arrange
        var repository = new ReviewRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var averageAndCount = await session.ExecuteWriteAsync(async tx =>
            await repository.GetAverageAndCountFromMovieId(tx, Database.MovieId));

        // Assert
        Assert.Equal(5, averageAndCount.Average);
        Assert.Equal(1, averageAndCount.Count);
    }
}