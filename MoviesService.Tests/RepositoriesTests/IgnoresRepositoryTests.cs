using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class IgnoresRepositoryTests
{
    public IgnoresRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })-[r:IGNORES]->(m:Movie { id: $movieId })
                             DELETE r
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        using var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters)).Wait();
    }

    private TestDatabaseSetup Database { get; }
    private IgnoresRepository Repository { get; } = new();

    [Fact]
    public async Task GetAllIgnoredMovies_ShouldReturnEmptyList_WhenUserHasNoIgnoredMovies()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllIgnoreMovies(tx, Database.UserId));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllIgnoredMovies_ShouldReturnIgnoredMovies_WhenUserHasIgnoredMovies()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:IGNORES]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var result = (await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllIgnoreMovies(tx, Database.UserId))).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(Database.MovieId);
    }

    [Fact]
    public async Task IgnoreMovie_ShouldCreateRelationship()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.IgnoreMovie(tx, Database.UserId, Database.MovieId));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:User { id: $userId })-[r:IGNORES]->(:Movie { id: $movieId })
                             RETURN COUNT(r) AS count
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var result = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        result.Should().Be(1);
    }

    [Fact]
    public async Task RemoveIgnoreMovie_ShouldDeleteRelationship()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:IGNORES]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await Repository.RemoveIgnoreMovie(tx, Database.UserId, Database.MovieId));

        // Assert

        // language=Cypher
        const string relationshipCountQuery = """
                                              MATCH (:User { id: $userId })-[r:IGNORES]->(:Movie { id: $movieId })
                                              RETURN COUNT(r) AS count
                                              """;

        var cursor = await session.RunAsync(relationshipCountQuery, parameters);
        var result = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        result.Should().Be(0);
    }

    [Fact]
    public async Task IgnoresExists_ShouldReturnTrue_WhenRelationshipExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)-[r:IGNORES]->(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.IgnoresExists(tx, Database.MovieId, Database.UserId));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IgnoresExists_ShouldReturnFalse_WhenRelationshipDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.IgnoresExists(tx, Database.MovieId, Database.UserId));

        // Assert
        result.Should().BeFalse();
    }
}