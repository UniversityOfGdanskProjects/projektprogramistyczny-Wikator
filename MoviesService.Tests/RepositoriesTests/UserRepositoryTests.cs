using FluentAssertions;
using MoviesService.Models;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class UserRepositoryTests
{
    public UserRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query =
            "CREATE (u:User { id: $userId, name: 'TestUser', email: 'test@gmail.com', lastActive: datetime(), activityScore: 1, role: 'User' })";

        var parameters = new { userId = UserId.ToString() };
        using var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters)).Wait();
    }

    private TestDatabaseSetup Database { get; }
    private Guid UserId { get; } = Guid.NewGuid();

    [Fact]
    public async Task GetUsersByMostActiveAsync_ShouldReturnUsersOrderedByActivityScore()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = (await
            session.ExecuteReadAsync(async tx => await userRepository.GetUsersByMostActiveAsync(tx))).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(UserId);
    }

    [Fact]
    public async Task GetUserActiveTodayCount_ShouldReturnCountOfUsersActiveToday()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var actual = await session.ExecuteReadAsync(async tx => await userRepository.GetUserActiveTodayCount(tx));

        // Assert
        actual.Should().Be(2);
    }

    [Fact]
    public async Task GetUserActiveTodayCount_ShouldReturnCountOfUsersActiveToday_WithUserNotActiveToday()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query =
            "CREATE (u:User { id: apoc.create.uuid(), name: 'TestUser', email: 'test2@gmail.com', lastActive: datetime({epochSeconds: $yesterdayEpoch}), activityScore: 1, role: 'User' })";
        await session.ExecuteWriteAsync(async tx =>
            await tx.RunAsync(query, new { yesterdayEpoch = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds() }));

        // Act
        var result = await session.ExecuteReadAsync(async tx => await userRepository.GetUserActiveTodayCount(tx));

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task UpdateUserNameAsync_ShouldUpdateUserName()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await userRepository.UpdateUserNameAsync(tx, UserId, "NewUsername");

            // language=Cypher
            const string query = "MATCH (u:User { id: $userId }) RETURN u.name AS name";
            var parameters = new { userId = UserId.ToString() };
            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => ValExtensions.ToString(record["name"]));
        });

        result.Should().Be("NewUsername");
    }

    [Fact]
    public async Task UpdateUserNameAsync_ShouldReturnUser()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteWriteAsync(async tx =>
            await userRepository.UpdateUserNameAsync(tx, UserId, "NewUsername"));

        // Assert
        result.Should().BeEquivalentTo(new User(
            UserId,
            "NewUsername",
            "test@gmail.com",
            "User"
        ));
    }

    [Fact]
    public async Task ChangeUserRoleToAdminAsync_ShouldChangeUserRoleToAdmin()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await userRepository.ChangeUserRoleToAdminAsync(tx, UserId);

            // language=Cypher
            const string query = "MATCH (u:User { id: $userId }) RETURN u.role AS role";
            var parameters = new { userId = UserId.ToString() };
            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync(record => ValExtensions.ToString(record["role"]));
        });

        result.Should().Be("Admin");
    }

    [Fact]
    public async Task UserExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx => await userRepository.UserExistsAsync(tx, UserId));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var userRepository = new UserRepository();
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result =
            await session.ExecuteReadAsync(async tx => await userRepository.UserExistsAsync(tx, Guid.NewGuid()));

        // Assert
        result.Should().BeFalse();
    }
}