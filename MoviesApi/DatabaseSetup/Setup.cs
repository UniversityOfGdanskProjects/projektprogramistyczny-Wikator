using System.Security.Cryptography;
using System.Text;
using Neo4j.Driver;

namespace MoviesApi.DatabaseSetup;

public class Setup(IDriver driver)
{
    private IDriver Driver { get; } = driver;

    public async Task SetupJobs()
    {
        await using var session = Driver.AsyncSession();
        // language=Cypher
        const string jobExistsQuery = "CALL apoc.periodic.list() YIELD name AS job";

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(jobExistsQuery);
            return await cursor.ToListAsync(record => record["job"].As<string>());
        });
        
        if (result.All(job => job != "decrease popularity"))
        {
            // language=Cypher
            const string createJobQuery = """
                                          CALL apoc.periodic.repeat(
                                              'decrease popularity',
                                              'MATCH (m:Movie) SET m.popularity = m.popularity / 2',
                                              43200000
                                          )
                                          """;
            await session.ExecuteWriteAsync(async tx => await tx.RunAsync(createJobQuery));
        }
    }

    public async Task CreateAdmin(IConfiguration config)
    {
        await using var sessions = Driver.AsyncSession();
        // language=Cypher
        const string adminExistQuery = "Match (u:User { Role : 'Admin' }) WITH COUNT(u) > 0 AS adminExists Return adminExists";
        
        var result = await sessions.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(adminExistQuery);
            return await cursor.SingleAsync(record => record["adminExists"].As<bool>());
        });

        if (!result)
        {
            using HMACSHA512 hmac = new();
            var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(config["InitialPassword"] ??
                                                                       throw new Exception("Initial password not found")));
            var passwordSalt = hmac.Key;

            // language=Cypher
            const string query = """
                                 CREATE (a:User {
                                   Id: randomUUID(),
                                   Name: $Name,
                                   Email: $Email,
                                   PasswordHash: $PasswordHash,
                                   PasswordSalt: $PasswordSalt,
                                   Role: "Admin",
                                   LastActive: datetime()
                                 })
                                 RETURN a.Name as name, a.Email as email, a.Role as role, a.Id as id
                                 """;

            await sessions.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, new
                {
                    Name = "Admin", Email = "wiktor@szymulewicz.com",
                    PasswordHash = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(passwordSalt)
                });
            });
        }
    }
}