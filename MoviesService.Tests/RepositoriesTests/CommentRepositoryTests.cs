using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class CommentRepositoryTests
{
    public CommentRepositoryTests(TestDatabaseSetup testDatabase)
    {
        Database = testDatabase;
        Database.SetupDatabase().Wait();

        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId })<-[r:COMMENTED]-(u:User { id: $userId })
                             OPTIONAL MATCH (m)-[n:NOTIFICATION]->(u)
                             DELETE r, n
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
    private CommentRepository RepositoryTests { get; } = new();

    [Fact]
    public async Task GetCommentAsync_ShouldReturnNull_WhenCommentDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetCommentAsync(tx, Guid.NewGuid()));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCommentAsync_ShouldReturnCommentDto_WhenCommentExists()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string query = """
                             MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                             CREATE (u)-[r:COMMENTED {
                               id: $commentId,
                               text: "comment",
                               createdAt: $dateTime,
                               isEdited: false
                             }]->(m)
                             """;

        var currentDateTime = DateTime.Now;
        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            commentId = commentId.ToString(),
            dateTime = currentDateTime
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetCommentAsync(tx, commentId));

        // Assert
        result.Should().NotBeNull();
        result!.Should().BeEquivalentTo(new CommentDto(
            commentId,
            Database.MovieId,
            Database.UserId,
            Database.AdminUserName,
            "comment",
            currentDateTime,
            false
        ));
    }

    [Fact]
    public async Task AddCommentAsync_ShouldCreateComment()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var addCommentDto = new AddCommentDto
        {
            Text = "comment",
            MovieId = Database.MovieId
        };

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.AddCommentAsync(tx, Database.UserId, addCommentDto));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })<-[r:COMMENTED]-(:User { id: $userId })
                             RETURN COUNT(r) AS count
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldCreateNotificationsToAllButCreator_IfMovieIsFavourite()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        // language=Cypher
        const string seedUsersQuery = """
                                      CREATE (u1:User { id: $user1Id, name: 'name 1' }), (u2:User { id: $user2Id, name: 'name 2' })
                                      WITH u1, u2
                                      MATCH (m:Movie { id: $movieId }), (a:User { id: $adminId })
                                      CREATE (u1)-[:FAVOURITE]->(m), (u2)-[:FAVOURITE]->(m), (a)-[:FAVOURITE]->(m)
                                      """;

        var seedUsersParameters = new
        {
            user1Id = user1Id.ToString(),
            user2Id = user2Id.ToString(),
            adminId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedUsersQuery, seedUsersParameters));

        var addCommentDto = new AddCommentDto
        {
            Text = "comment",
            MovieId = Database.MovieId
        };

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.AddCommentAsync(tx, Database.UserId, addCommentDto));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })-[r:NOTIFICATION]->(:User)
                             RETURN COUNT(r) AS count
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(2);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldNotCreateNotifications_IfMovieIsNotFavourite()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        var addCommentDto = new AddCommentDto
        {
            Text = "comment",
            MovieId = Database.MovieId
        };

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.AddCommentAsync(tx, Database.UserId, addCommentDto));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })-[r:NOTIFICATION]->(:User)
                             RETURN COUNT(r) AS count
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldNotCreateNotifications_IfMovieIsFavouriteOnlyByCreator()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string seedRelationQuery = """
                                         MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                         CREATE (u)-[:FAVOURITE]->(m)
                                         """;

        var seedRelationParameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedRelationQuery, seedRelationParameters));

        var addCommentDto = new AddCommentDto
        {
            Text = "comment",
            MovieId = Database.MovieId
        };

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.AddCommentAsync(tx, Database.UserId, addCommentDto));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })-[r:NOTIFICATION]->(:User)
                             RETURN COUNT(r) AS count
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldReturnCommentDto()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var addCommentDto = new AddCommentDto
        {
            Text = "comment",
            MovieId = Database.MovieId
        };

        // Act
        var result = await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.AddCommentAsync(tx, Database.UserId, addCommentDto));

        // Assert

        result.Comment.MovieId.Should().Be(Database.MovieId);
        result.Comment.UserId.Should().Be(Database.UserId);
        result.Comment.Username.Should().Be(Database.AdminUserName);
        result.Comment.Text.Should().Be("comment");
        result.Comment.IsEdited.Should().BeFalse();
        result.Comment.CreatedAt.Should().BeCloseTo(DateTime.Now, new TimeSpan(0, 0, 10));
        result.Notification.MovieId.Should().Be(Database.MovieId);
        result.Notification.CommentUsername.Should().Be(Database.AdminUserName);
        result.Notification.CommentText.Should().Be("comment");
        result.Notification.MovieTitle.Should().Be("The Matrix");
    }

    [Fact]
    public async Task EditCommentAsync_ShouldUpdateNode()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                 CREATE (u)-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            commentId = commentId.ToString(),
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        var editCommentDto = new EditCommentDto
        {
            Text = "edited comment"
        };

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.EditCommentAsync(tx, commentId, Database.UserId, editCommentDto));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie)<-[r:COMMENTED { id: $commentId }]-(:User)
                             RETURN r.text AS text, r.isEdited AS isEdited
                             """;

        var parameters = new
        {
            commentId = commentId.ToString()
        };

        var cursor = await session.RunAsync(query, parameters);
        var record = await cursor.SingleAsync();
        record["text"].Should().Be("edited comment");
        record["isEdited"].Should().Be(true);
    }

    [Fact]
    public async Task EditCommentAsync_ShouldReturnCommentDto()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                 CREATE (u)-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            commentId = commentId.ToString(),
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        var editCommentDto = new EditCommentDto
        {
            Text = "edited comment"
        };

        // Act
        var result = await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.EditCommentAsync(tx, commentId, Database.UserId, editCommentDto));

        // Assert

        result.MovieId.Should().Be(Database.MovieId);
        result.UserId.Should().Be(Database.UserId);
        result.Username.Should().Be(Database.AdminUserName);
        result.Text.Should().Be("edited comment");
        result.IsEdited.Should().BeTrue();
    }

    [Fact]
    public async Task CommentExistsAsOwnerOrAdmin_ShouldReturnFalse_WhenCommentDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.CommentExistsAsOwnerOrAdmin(tx, Guid.NewGuid(), Database.UserId));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CommentExistsAsOwnerOrAdmin_ShouldReturnTrue_IfUserCommentExistsAsUser()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $userId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.CommentExistsAsOwnerOrAdmin(tx, commentId, userId));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CommentExistsAsOwnerOrAdmin_ShouldReturnTrue_IfUserCommentExistsAsAdmin()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $userId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.CommentExistsAsOwnerOrAdmin(tx, commentId, Database.UserId));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CommentExistsAsOwnerOrAdmin_ShouldReturnFalse_IfAdminCommentExistsAsUser()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $adminId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m), (:User { id: $userId})
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            adminId = Database.UserId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.CommentExistsAsOwnerOrAdmin(tx, commentId, userId));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldDeleteNode()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                 CREATE (u)-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.DeleteCommentAsync(tx, commentId, Database.UserId));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })<-[r:COMMENTED { id: $commentId }]-(:User)
                             RETURN COUNT(r) AS count
                             """;

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldDeleteNotifications()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                 CREATE (u)-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 WITH m
                                 CREATE
                                   (m)-[:NOTIFICATION { relatedEntityId: $commentId }]->(:User),
                                   (m)-[:NOTIFICATION { relatedEntityId: $commentId }]->(:User)
                                 """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.DeleteCommentAsync(tx, commentId, Database.UserId));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie { id: $movieId })-[r:NOTIFICATION { relatedEntityId: $commentId }]->(:User)
                             RETURN COUNT(r) AS count
                             """;

        var cursor = await session.RunAsync(query, parameters);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldNotDeleteOtherNotifications()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId }), (u:User { id: $userId })
                                 CREATE (u)-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 WITH m
                                 CREATE
                                   (m)-[:NOTIFICATION { relatedEntityId: $commentId }]->(:User),
                                   (m)-[:NOTIFICATION { relatedEntityId: $commentId }]->(:User),
                                   (:Movie)-[:NOTIFICATION { relatedEntityId: apoc.create.uuid() }]->(:User),
                                   (m)-[:NOTIFICATION { relatedEntityId: apoc.create.uuid() }]->(:User)
                                 """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, parameters));

        // Act
        await session.ExecuteWriteAsync(async tx =>
            await RepositoryTests.DeleteCommentAsync(tx, commentId, Database.UserId));

        // Assert

        // language=Cypher
        const string query = """
                             MATCH (:Movie)-[r:NOTIFICATION]->(:User)
                             RETURN COUNT(r) AS count
                             """;

        var cursor = await session.RunAsync(query);
        var count = await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetMovieIdFromCommentAsOwnerOrAdminAsync_ShouldNotReturnId_WhenCommentDoesNotExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetMovieIdFromCommentAsOwnerOrAdminAsync(tx, Guid.NewGuid(), Database.UserId));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMovieIdFromCommentAsOwnerOrAdminAsync_ShouldReturnId_IfUserCommentExistsAsUser()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $userId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetMovieIdFromCommentAsOwnerOrAdminAsync(tx, commentId, userId));

        // Assert
        result.Should().Be(Database.MovieId);
    }

    [Fact]
    public async Task GetMovieIdFromCommentAsOwnerOrAdminAsync_ShouldReturnId_IfUserCommentExistsAsAdmin()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var userId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $userId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m)
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetMovieIdFromCommentAsOwnerOrAdminAsync(tx, commentId, Database.UserId));

        // Assert
        result.Should().Be(Database.MovieId);
    }

    [Fact]
    public async Task GetMovieIdFromCommentAsOwnerOrAdminAsync_ShouldNotReturnId_IfAdminCommentExistsAsUser()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // language=Cypher
        const string seedQuery = """
                                 MATCH (m:Movie { id: $movieId })
                                 CREATE (:User { id: $adminId })-[:COMMENTED {
                                   id: $commentId,
                                   text: "comment",
                                   createdAt: $dateTime,
                                   isEdited: false
                                 }]->(m), (:User { id: $userId})
                                 """;

        var seedParameters = new
        {
            movieId = Database.MovieId.ToString(),
            adminId = Database.UserId.ToString(),
            userId = userId.ToString(),
            commentId = commentId.ToString(),
            dateTime = DateTime.Now
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(seedQuery, seedParameters));

        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await RepositoryTests.GetMovieIdFromCommentAsOwnerOrAdminAsync(tx, commentId, userId));

        // Assert
        result.Should().BeNull();
    }
}