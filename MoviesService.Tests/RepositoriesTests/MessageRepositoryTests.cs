using FluentAssertions;

namespace MoviesService.Tests.RepositoriesTests;

[Collection("DatabaseCollection")]
public class MessageRepositoryTests
{
    public MessageRepositoryTests(TestDatabaseSetup database)
    {
        Database = database;
        Database.SetupDatabase().Wait();
        using var session = Database.Driver.AsyncSession();
        
        // language=Cypher
        const string query = """
                             MATCH (m:Message)
                             DETACH DELETE m
                             """;

        session.ExecuteWriteAsync(async tx => await tx.RunAsync(query)).Wait();
        session.CloseAsync().Wait();
    }
    
    private TestDatabaseSetup Database { get; }
    private MessageRepository Repository { get; } = new();

    [Fact]
    public async Task CreateMessageAsync_ShouldCreateNode()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.CreateMessageAsync(tx, Database.UserId, "Hi");
            
            // language=Cypher
            const string query = """
                                 MATCH (m:Message)
                                 RETURN COUNT(m) as count
                                 """;

            var cursor = await tx.RunAsync(query);
            return await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        });

        result.Should().Be(1);
    }
    
    [Fact]
    public async Task CreateMessageAsync_ShouldCreateRelationship()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // Act and Assert
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            await Repository.CreateMessageAsync(tx, Database.UserId, "Hi");
            
            // language=Cypher
            const string query = """
                                 MATCH (:Message)<-[r:SENT]-(:User)
                                 RETURN COUNT(r) as count
                                 """;

            var cursor = await tx.RunAsync(query);
            return await cursor.SingleAsync(record => ValExtensions.ToInt(record["count"]));
        });

        result.Should().Be(1);
    }

    [Fact]
    public async Task CreateMessageAsync_ShouldReturnDto()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        
        // Act
        var result = await session.ExecuteWriteAsync(async tx =>
            await Repository.CreateMessageAsync(tx, Database.UserId, "Hi"));
        
        // Arrange
        result.UserName.Should().Be("Admin");
        result.Content.Should().Be("Hi");
        result.Date.Should().BeCloseTo(DateTime.Now, new TimeSpan(0, 0, 3));
    }

    [Fact]
    public async Task GetMostRecentMessagesAsync_ShouldReturnSortedEntities()
    {
        // Arrange
        await using var session = Database.Driver.AsyncSession();
        var dateTimeNow1 = DateTime.Now;
        var dateTimeNow2 = DateTime.Now;
        var dateTimeNow3 = DateTime.Now;
        
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             CREATE (:Message {content: "Hi1", createdAt: $messageDate1})<-[:SENT]-(u),
                               (:Message {content: "Hi3", createdAt: $messageDate3})<-[:SENT]-(u),
                               (:Message {content: "Hi2", createdAt: $messageDate2})<-[:SENT]-(u)
                             """;

        var parameters = new
        {
            userId = Database.UserId.ToString(),
            messageDate1 = dateTimeNow1,
            messageDate2 = dateTimeNow2,
            messageDate3 = dateTimeNow3
        };

        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(query, parameters));

        IEnumerable<MessageDto> expectedResult =
        [
            new MessageDto("Hi3", "Admin", dateTimeNow3),
            new MessageDto("Hi2", "Admin", dateTimeNow2),
            new MessageDto("Hi1", "Admin", dateTimeNow1)
        ];
        
        // Act
        var result = await session.ExecuteReadAsync(async tx => await Repository.GetMostRecentMessagesAsync(tx));
        
        // Arrange
        result.Should().BeEquivalentTo(expectedResult);
    }
}