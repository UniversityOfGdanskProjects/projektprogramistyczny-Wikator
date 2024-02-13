using System.Security.Cryptography;
using System.Text;
using MoviesService.DataAccess.Extensions;
using MoviesService.DataAccess.Repositories.Contracts;
using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories;

public class AccountRepository : IAccountRepository
{
    public async Task<User> RegisterAsync(IAsyncQueryRunner tx, RegisterDto registerDto)
    {
        using HMACSHA512 hmac = new();
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        var passwordSalt = hmac.Key;

        // language=Cypher
        const string query = """
                             CREATE (a:User {
                               id: apoc.create.uuid(),
                               name: $name,
                               email: $email,
                               passwordHash: $passwordHash,
                               passwordSalt: $passwordSalt,
                               role: "User",
                               lastActive: datetime(),
                               activityScore: 0
                             })
                             RETURN
                               a.id AS id,
                               a.name AS name,
                               a.email AS email,
                               a.role AS role
                             """;

        var parameters = new
        {
            name = registerDto.Name,
            email = registerDto.Email,
            passwordHash = Convert.ToBase64String(passwordHash),
            passwordSalt = Convert.ToBase64String(passwordSalt)
        };

        var cursor = await tx.RunAsync(query, parameters);
        return await cursor.SingleAsync(record => record.ConvertToUser());
    }

    public async Task<User?> LoginAsync(IAsyncQueryRunner tx, LoginDto loginDto)
    {
        try
        {
            // language=Cypher
            const string query = """
                                 MATCH (a:User { email: $email })
                                 RETURN
                                   a.id AS id,
                                   a.name AS name,
                                   a.email AS email,
                                   a.role AS role,
                                   a.passwordHash AS passwordHash,
                                   a.passwordSalt AS passwordSalt
                                 """;

            var cursor = await tx.RunAsync(query, new { email = loginDto.Email });
            var record = await cursor.SingleAsync();

            if (record is null)
                return null;

            var storedPasswordHash = record["passwordHash"].As<string>();
            var storedPasswordSalt = record["passwordSalt"].As<string>();

            using HMACSHA512 hmac = new(Convert.FromBase64String(storedPasswordSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            var storedHash = Convert.FromBase64String(storedPasswordHash);

            return computedHash.Where((t, i) => t != storedHash[i]).Any() ? null : record.ConvertToUser();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task DeleteUserAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $Id })
                             DETACH DELETE u
                             """;

        await tx.RunAsync(query, new { Id = userId.ToString() });
    }

    public async Task<bool> EmailExistsAsync(IAsyncQueryRunner tx, string email)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { email: $email })
                             WITH COUNT(u) > 0  as node_exists
                             RETURN node_exists
                             """;

        var accountCursor = await tx.RunAsync(query, new { email });
        var result = await accountCursor.SingleAsync();
        return result["node_exists"].As<bool>();
    }
}