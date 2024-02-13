using System.Security.Cryptography;
using System.Text;
using Neo4j.Driver;

namespace MoviesService.DataAccess;

public class DatabaseSetup(IDriver driver)
{
    private IDriver Driver { get; } = driver;

    public async Task SetupJobs()
    {
        await using var session = Driver.AsyncSession();

        // language=Cypher
        const string jobExists = "CALL apoc.periodic.list() YIELD name AS job";

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(jobExists);
            return await cursor.ToListAsync(record => record["job"].As<string>());
        });

        if (result.All(job => job != "decrease popularity"))
        {
            // language=Cypher
            const string createPopularityJobQuery = """
                                                    CALL apoc.periodic.repeat(
                                                        'decrease popularity',
                                                        'MATCH (m:Movie) SET m.popularity = m.popularity / 2',
                                                        60 * 60 * 12
                                                    )
                                                    """;

            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(createPopularityJobQuery));
        }

        if (result.All(job => job != "decrease activity score"))
        {
            // language=Cypher
            const string createActivityScoreJobQuery = """
                                                       CALL apoc.periodic.repeat(
                                                           'decrease activity score',
                                                           'MATCH (u:User) SET u.activityScore = u.activityScore / 2',
                                                           60 * 60 * 24 * 3
                                                       )
                                                       """;

            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(createActivityScoreJobQuery));
        }

        if (result.All(job => job != "delete old messages"))
        {
            // language=Cypher
            const string createDeleteOldMessagesJobQuery = """
                                                           CALL apoc.periodic.repeat(
                                                               'delete old messages',
                                                               'MATCH (m:Message)
                                                                WHERE datetime(m.createdAt) < datetime() - duration("P7D")
                                                                DETACH DELETE m',
                                                               60 * 60 * 24
                                                           )
                                                           """;

            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(createDeleteOldMessagesJobQuery));
        }
    }

    public async Task CreateAdmin(string initialPassword)
    {
        await using var sessions = Driver.AsyncSession();

        // language=Cypher
        const string adminExistQuery = "Match (u:User { role : 'Admin' }) RETURN COUNT(u) > 0 AS adminExists";

        var result = await sessions.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(adminExistQuery);
            return await cursor.SingleAsync(record => record["adminExists"].As<bool>());
        });

        if (!result)
        {
            using HMACSHA512 hmac = new();
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(initialPassword));
            var passwordSalt = hmac.Key;

            // language=Cypher
            const string query = """
                                 CREATE (a:User {
                                   id: apoc.create.uuid(),
                                   name: $name,
                                   email: $email,
                                   passwordHash: $passwordHash,
                                   passwordSalt: $passwordSalt,
                                   role: "Admin",
                                   lastActive: datetime()
                                 })
                                 RETURN a.name as name, a.email as email, a.role as role, a.id as id
                                 """;

            var parameters = new
            {
                name = "Admin",
                email = "wiktor@szymulewicz.com",
                passwordHash = Convert.ToBase64String(passwordHash),
                passwordSalt = Convert.ToBase64String(passwordSalt)
            };

            await sessions.ExecuteWriteAsync(async tx => { await tx.RunAsync(query, parameters); });
        }
    }

    public async Task SeedGenres()
    {
        await using var session = Driver.AsyncSession();

        //language=Cypher
        const string query = """
                             UNWIND $genres AS genre
                             MERGE (g:Genre { name: genre })
                             RETURN g.name as name
                             """;

        HashSet<string> genres =
        [
            "Action", "Adventure", "Animation", "Comedy", "Crime", "Documentary", "Drama", "Family", "Fantasy",
            "History", "Horror", "Music", "Mystery", "Romance", "Science Fiction", "TV Movie", "Thriller", "War",
            "Western"
        ];

        await session.ExecuteWriteAsync(tx => tx.RunAsync(query, new { genres }));
    }
}