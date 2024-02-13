using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class AccountRepositoryTests
{
    public AccountRepositoryTests(TestDatabaseSetup database)
    {
        Database = database;
        Database.SetupDatabase().Wait();
        using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User)
                             WHERE u.id <> $userId
                             DELETE u
                             """;

        var parameters = new { userId = Database.UserId.ToString() };
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters)).Wait();
        session.CloseAsync().Wait();
    }

    private TestDatabaseSetup Database { get; }
    private AccountRepository Repository { get; } = new();

    [Fact]
    public async Task RegisterAsync_ShouldCreateNode()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var registerDto = new RegisterDto
        {
            Name = "Test User",
            Email = "test@gmail.com",
            Password = "password"
        };

        // Act and Assert
        var user = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.RegisterAsync(tx, registerDto);

            // language=Cypher
            const string query = """
                                 MATCH (u:User { email: $email })
                                 RETURN
                                   u.id AS id,
                                   u.name AS name,
                                   u.email AS email,
                                   u.role AS role
                                 """;

            var parameters = new { email = registerDto.Email };
            var cursor = await tx.RunAsync(query, parameters);
            return await cursor.SingleAsync();
        });

        ValExtensions.ToString(user["id"]).Should().NotBeNullOrEmpty();
        ValExtensions.ToString(user["name"]).Should().Be(registerDto.Name);
        ValExtensions.ToString(user["email"]).Should().Be(registerDto.Email);
        ValExtensions.ToString(user["role"]).Should().Be("User");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNewUser()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var registerDto = new RegisterDto
        {
            Name = "Test User",
            Email = "test@gmail.com",
            Password = "password"
        };

        // Act
        var user = await session.ExecuteWriteAsync(async tx => await Repository.RegisterAsync(tx, registerDto));

        // Assert
        user.Email.Should().Be(registerDto.Email);
        user.Name.Should().Be(registerDto.Name);
        user.Role.Should().Be("User");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnUser_IfExistsAndPasswordsMatch()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var loginDto = new LoginDto
        {
            Email = "wiktor@szymulewicz.com",
            Password = "Pa$$w0rd"
        };

        // Act
        var user = await session.ExecuteWriteAsync(async tx => await Repository.LoginAsync(tx, loginDto));

        // Assert
        user.Should().NotBeNull();
        user!.Email.Should().Be(loginDto.Email);
        user.Name.Should().Be("Admin");
        user.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_IfNotExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var loginDto = new LoginDto
        {
            Email = "test@gmail.com",
            Password = "Pa$$w0rd"
        };

        // Act
        var user = await session.ExecuteWriteAsync(async tx => await Repository.LoginAsync(tx, loginDto));

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_IfPasswordsDoNotMatch()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var loginDto = new LoginDto
        {
            Email = "admin@gmail.com",
            Password = "password"
        };

        // Act
        var user = await session.ExecuteWriteAsync(async tx => await Repository.LoginAsync(tx, loginDto));

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldDeleteNode()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        await session.ExecuteWriteAsync(async tx => await Repository.DeleteUserAsync(tx, Database.UserId));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             RETURN COUNT(u) > 0 AS exists
                             """;

        var parameters = new { userId = Database.UserId.ToString() };
        var cursor = await session.RunAsync(query, parameters);
        var result = await cursor.SingleAsync(record => ValExtensions.ToBool(record["exists"]));
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_IfEmailExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var exists =
            await session.ExecuteWriteAsync(async tx =>
                await Repository.EmailExistsAsync(tx, "wiktor@szymulewicz.com"));

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_IfEmailDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var exists =
            await session.ExecuteWriteAsync(async tx =>
                await Repository.EmailExistsAsync(tx, "test@gmail.com"));

        // Assert
        exists.Should().BeFalse();
    }
}