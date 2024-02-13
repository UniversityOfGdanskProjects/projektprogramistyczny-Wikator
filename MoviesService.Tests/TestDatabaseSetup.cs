using System.Security.Cryptography;
using System.Text;
using MoviesService.DataAccess;

namespace MoviesService.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class TestDatabaseSetup : IDisposable, IClassFixture<TestDatabaseSetup>
{
    public IDriver Driver { get; } = GraphDatabase.Driver(
        TestConfiguration.Neo4JUri,
        AuthTokens.None);

    public Guid UserId { get; } = Guid.NewGuid();
    public Guid MovieId { get; } = Guid.NewGuid();
    public string AdminUserName { get; } = "Admin";

    public void Dispose()
    {
        var session = Driver.AsyncSession();
        const string deleteAllQuery = "MATCH (n) DETACH DELETE n";
        session.ExecuteWriteAsync(async tx => await tx.RunAsync(deleteAllQuery)).Wait();
        Driver.Dispose();
    }


    public async Task SetupDatabase()
    {
        await using var session = Driver.AsyncSession();

        const string deleteAllQuery = "MATCH (n) DETACH DELETE n";
        await session.ExecuteWriteAsync(async tx => await tx.RunAsync(deleteAllQuery));

        var setup = new DatabaseSetup(Driver);
        await setup.SetupJobs();
        await setup.SeedGenres();

        // language=Cypher
        const string movieQuery = """
                                  CREATE (m:Movie {
                                    id: $id,
                                    title: "The Matrix",
                                    description: "Description",
                                    pictureAbsoluteUri: NULL,
                                    picturePublicId: NULL,
                                    inTheaters: false,
                                    releaseDate: datetime("2000-01-01"),
                                    minimumAge: 13,
                                    popularity: 0,
                                    trailerAbsoluteUri: NULL
                                  })
                                  """;


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
                                    lastActive: datetime(),
                                    activityScore: 0
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

        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(movieQuery, new { id = MovieId.ToString() });
            await tx.RunAsync(adminQuery, adminParameters);
        });
    }
}