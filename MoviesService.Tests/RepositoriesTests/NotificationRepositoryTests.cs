using FluentAssertions;
using MoviesService.Models.Parameters;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class NotificationRepositoryTests
{
    public NotificationRepositoryTests(TestDatabaseSetup database)
    {
        Database = database;

        // language=Cypher
        const string query = """
                             MATCH (:User)<-[r:NOTIFICATION]-(:Movie)
                             DELETE r
                             """;

        database.SetupDatabase().Wait();
        using var session = Database.Driver.AsyncSession();
        session.ExecuteWriteAsync(tx => tx.RunAsync(query)).Wait();
    }

    private TestDatabaseSetup Database { get; }
    private NotificationRepository Repository { get; } = new();

    [Fact]
    public async Task GetAllNotifications_ShouldReturnEmptyList_WhenNoNotificationsExist()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // Act
        var notifications = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllNotificationsAsync(tx, new NotificationQueryParams(), Database.UserId));

        // Assert
        notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllNotifications_ShouldReturnNotifications_WhenNotificationsExist()
    {
        // Arrange
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();
        var comment1Id = Guid.NewGuid();
        var comment2Id = Guid.NewGuid();
        var notification1Id = Guid.NewGuid();
        var notification2Id = Guid.NewGuid();
        var date = DateTime.Now;
        await using var session = Database.Driver.AsyncSession();

        var expectedNotification1 = new NotificationDto(notification1Id, false, date, "user2", "comment1",
            Database.MovieId, "The Matrix");
        var expectedNotification2 = new NotificationDto(notification2Id, true, date, "user3", "comment2",
            Database.MovieId, "The Matrix");

        // language=Cypher
        const string query = """
                             CREATE(u2:User { id: $user2Id, name: 'user2' }), (u3:User { id: $user3Id, name: 'user3' })
                             WITH u2, u3
                             MATCH (m:Movie { id: $movieId })
                             CREATE(u2)-[:COMMENTED { id: $comment1Id, text: 'comment1', createdAt: $date, isEdited: false }]->(m),
                                (u3)-[:COMMENTED { id: $comment2Id, text: 'comment2', createdAt: $date, isEdited: true }]->(m)
                             WITH m
                             MATCH (u:User { id: $userId })
                             CREATE (u)<-[:NOTIFICATION { id: $notification1Id, isRead: false, relatedEntityId: $comment1Id }]-(m),
                                (u)<-[:NOTIFICATION { id: $notification2Id, isRead: true, relatedEntityId: $comment2Id }]-(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            user2Id = user2Id.ToString(),
            user3Id = user3Id.ToString(),
            comment1Id = comment1Id.ToString(),
            comment2Id = comment2Id.ToString(),
            notification1Id = notification1Id.ToString(),
            notification2Id = notification2Id.ToString(),
            date
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var notifications = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllNotificationsAsync(tx, new NotificationQueryParams(), Database.UserId));

        // Assert
        notifications.Items.Should().ContainEquivalentOf(expectedNotification1);
        notifications.Items.Should().ContainEquivalentOf(expectedNotification2);
    }

    [Fact]
    public async Task GetAllNotifications_ShouldReturnSortedAndPaginatedNotifications_WhenNotificationsExist()
    {
        // Arrange
        var user2Id = Guid.NewGuid();
        var user3Id = Guid.NewGuid();
        var comment1Id = Guid.NewGuid();
        var comment2Id = Guid.NewGuid();
        var comment3Id = Guid.NewGuid();
        var notification1Id = Guid.NewGuid();
        var notification2Id = Guid.NewGuid();
        var notification3Id = Guid.NewGuid();
        var date = DateTime.Now;
        await using var session = Database.Driver.AsyncSession();

        var expectedNotification = new NotificationDto(notification2Id, true, date, "user3", "comment2",
            Database.MovieId, "The Matrix");

        // language=Cypher
        const string query = """
                             CREATE(u2:User { id: $user2Id, name: 'user2' }), (u3:User { id: $user3Id, name: 'user3' })
                             WITH u2, u3
                             MATCH (m:Movie { id: $movieId })
                             CREATE(u2)-[:COMMENTED { id: $comment1Id, text: 'comment1', createdAt: $date, isEdited: true }]->(m),
                                (u3)-[:COMMENTED { id: $comment2Id, text: 'comment2', createdAt: $date, isEdited: false }]->(m),
                                (u3)-[:COMMENTED { id: $comment3Id, text: 'comment2', createdAt: $date, isEdited: true }]->(m)
                             WITH m
                             MATCH (u:User { id: $userId })
                             CREATE (u)<-[:NOTIFICATION { id: $notification1Id, isRead: false, relatedEntityId: $comment1Id }]-(m),
                                (u)<-[:NOTIFICATION { id: $notification2Id, isRead: true, relatedEntityId: $comment2Id }]-(m),
                                (u)<-[:NOTIFICATION { id: $notification3Id, isRead: false, relatedEntityId: $comment2Id }]-(m)
                             """;

        var parameters = new
        {
            movieId = Database.MovieId.ToString(),
            userId = Database.UserId.ToString(),
            user2Id = user2Id.ToString(),
            user3Id = user3Id.ToString(),
            comment1Id = comment1Id.ToString(),
            comment2Id = comment2Id.ToString(),
            comment3Id = comment3Id.ToString(),
            notification1Id = notification1Id.ToString(),
            notification2Id = notification2Id.ToString(),
            notification3Id = notification3Id.ToString(),
            date
        };

        var queryParams1 = new NotificationQueryParams { PageNumber = 2, PageSize = 2 };
        var queryParams2 = new NotificationQueryParams { PageNumber = 1, PageSize = 2 };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act
        var notifications1 = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllNotificationsAsync(tx, queryParams1, Database.UserId));

        var notifications2 = await session.ExecuteReadAsync(async tx =>
            await Repository.GetAllNotificationsAsync(tx, queryParams2, Database.UserId));

        // Assert
        notifications1.Items.Should().HaveCount(1);
        notifications1.Items.Should().ContainEquivalentOf(expectedNotification);
        notifications2.Items.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MarkNotificationAsRead_ShouldUpdateNotification(bool isInitiallyRead)
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)<-[:NOTIFICATION { id: $notificationId, isRead: $initialIsRead, relatedEntityId: apoc.create.uuid() }]-(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            notificationId = notificationId.ToString(),
            movieId = Database.MovieId.ToString(),
            initialIsRead = isInitiallyRead
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.MarkNotificationAsReadAsync(tx, notificationId, Database.UserId);

            // language=Cypher
            const string readQuery = """
                                     MATCH (:User)<-[r:NOTIFICATION { id: $notificationId }]-(:Movie)
                                     RETURN r.isRead AS isRead
                                     """;

            var readParameters = new
            {
                notificationId = notificationId.ToString()
            };

            var readCursor = await tx.RunAsync(readQuery, readParameters);
            return await readCursor.SingleAsync(record => ValExtensions.ToBool(record["isRead"]));
        });

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MarkAllNotificationsAsRead_ShouldUpdateAllNotifications(bool isInitiallyRead)
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)<-[:NOTIFICATION { id: apoc.create.uuid(), isRead: $initialIsRead, relatedEntityId: apoc.create.uuid() }]-(m),
                                (u)<-[:NOTIFICATION { id: apoc.create.uuid(), isRead: $initialIsRead, relatedEntityId: apoc.create.uuid() }]-(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString(),
            initialIsRead = isInitiallyRead
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.MarkAllNotificationsAsReadAsync(tx, Database.UserId);

            // language=Cypher
            const string readQuery = """
                                     MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(:Movie)
                                     RETURN COUNT(r) AS allCount, SUM(CASE WHEN r.isRead THEN 1 ELSE 0 END) AS readCount
                                     """;

            var readParameters = new
            {
                userId = Database.UserId.ToString()
            };

            var readCursor = await tx.RunAsync(readQuery, readParameters);
            return await readCursor.SingleAsync(record =>
                ValExtensions.ToInt(record["allCount"]) == ValExtensions.ToInt(record["readCount"]));
        });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteNotification_ShouldDeleteNotification()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)<-[:NOTIFICATION { id: $notificationId, isRead: false, relatedEntityId: apoc.create.uuid() }]-(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            notificationId = notificationId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.DeleteNotificationAsync(tx, notificationId, Database.UserId);

            // language=Cypher
            const string existsQuery = """
                                       MATCH (:User { id: $userId })<-[r:NOTIFICATION { id: $notificationId }]-(:Movie)
                                       RETURN COUNT(r) > 0 AS exists
                                       """;

            var existsParameters = new
            {
                userId = Database.UserId.ToString(),
                notificationId = notificationId.ToString()
            };

            var existsCursor = await tx.RunAsync(existsQuery, existsParameters);
            return !await existsCursor.SingleAsync(record => ValExtensions.ToBool(record["exists"]));
        });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAllNotifications_ShouldDeleteAllNotifications()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m:Movie { id: $movieId })
                             CREATE (u)<-[:NOTIFICATION { id: apoc.create.uuid(), isRead: false, relatedEntityId: apoc.create.uuid() }]-(m),
                                (u)<-[:NOTIFICATION { id: apoc.create.uuid(), isRead: false, relatedEntityId: apoc.create.uuid() }]-(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString()
        };

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, parameters));

        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.DeleteAllNotificationsAsync(tx, Database.UserId);

            // language=Cypher
            const string existsQuery = """
                                       MATCH (:User { id: $userId })<-[r:NOTIFICATION]-(:Movie)
                                       RETURN COUNT(r) > 0 AS exists
                                       """;

            var existsParameters = new
            {
                userId = Database.UserId.ToString()
            };

            var existsCursor = await tx.RunAsync(existsQuery, existsParameters);
            return !await existsCursor.SingleAsync(record => ValExtensions.ToBool(record["exists"]));
        });

        result.Should().BeTrue();
    }

    [Fact]
    public async Task NotificationExistsAsync_ShouldReturnTrue_IfNotificationExists()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        await using var session = Database.Driver.AsyncSession();

        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId }), (m: Movie { id: $movieId })
                             CREATE (u)<-[:NOTIFICATION { id: $notificationId }]-(m)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            movieId = Database.MovieId.ToString(),
            notificationId = notificationId.ToString()
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));
        
        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.NotificationExistsAsync(tx, notificationId, Database.UserId));
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task NotificationExistsAsync_ShouldReturnFalse_IfNotificationDoesNotExist()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var result = await session.ExecuteReadAsync(async tx =>
            await Repository.NotificationExistsAsync(tx, notificationId, Database.UserId));
        
        // Assert
        result.Should().BeFalse();
    }
}
