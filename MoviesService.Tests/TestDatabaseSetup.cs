using System.Security.Cryptography;
using System.Text;
using MoviesService.DataAccess;
using Neo4j.Driver;

namespace MoviesService.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseSetup : IDisposable, IClassFixture<TestDatabaseSetup>
{
    public IDriver Driver { get; }
    public Guid UserId { get; } = Guid.NewGuid();
    public Guid MovieId { get; } = Guid.NewGuid();

    public TestDatabaseSetup()
    {
        Driver = GraphDatabase.Driver(
            TestConfiguration.Neo4JUri,
            AuthTokens.Basic(TestConfiguration.Neo4JUser, TestConfiguration.Neo4JPassword));
        
        var session = Driver.AsyncSession();
        
        var setup = new DatabaseSetup(Driver);
        setup.SetupJobs().Wait();
        setup.SeedGenres().Wait();
        
        const string movieQuery = "CREATE (m:Movie {id: $id, title: 'Test Movie'})";
        using HMACSHA512 hmac = new();
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("Pa$$w0rd"));
        var passwordSalt = hmac.Key;

        // language=Cypher
        const string adminQuery = """
                             CREATE (a:User {
                               id: $id,
                               name: $name,
                               email: $email,
                               passwordHash: $passwordHash,
                               passwordSalt: $passwordSalt,
                               role: "Admin",
                               lastActive: datetime()
                             })
                             RETURN a.name as name, a.email as email, a.role as role, a.id as id
                             """;

        var adminParameters = new
        {
            id = UserId.ToString(),
            name = "Admin",
            email = "wiktor@szymulewicz.com",
            passwordHash = Convert.ToBase64String(passwordHash),
            passwordSalt = Convert.ToBase64String(passwordSalt)
        };
        
        session = Driver.AsyncSession();

        session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(movieQuery, new { id = MovieId.ToString() });
            await tx.RunAsync(adminQuery, adminParameters);
        });
    }

    public void Dispose()
    {
        var session = Driver.AsyncSession();
        const string deleteAllQuery = "MATCH (n) DETACH DELETE n";
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(deleteAllQuery)).Wait();
        Driver.Dispose();
    }
}