using System.Security.Cryptography;
using System.Text;
using MoviesApi.DTOs.Requests;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class AccountRepository : IAccountRepository
{
    public async Task<User?> RegisterAsync(IAsyncQueryRunner tx, RegisterDto registerDto)
    {
        using HMACSHA512 hmac = new();
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        var passwordSalt = hmac.Key;

        // language=Cypher
        const string query = """
                             CREATE (a:User {
                               Id: randomUUID(),
                               Name: $Name,
                               Email: $Email,
                               PasswordHash: $PasswordHash,
                               PasswordSalt: $PasswordSalt,
                               Role: "User",
                               LastActive: datetime()
                             })
                             RETURN a.Name as name, a.Email as email, a.Role as role, a.Id as id
                             """;

        var cursor = await tx.RunAsync(query, new
        {
            registerDto.Name, registerDto.Email,
            PasswordHash = Convert.ToBase64String(passwordHash), PasswordSalt = Convert.ToBase64String(passwordSalt)
        });
        var node = await cursor.SingleAsync();

        return GetUserFromNode(node);
    }

    public async Task<User?> LoginAsync(IAsyncQueryRunner tx, LoginDto loginDto)
    {
        try
        {
            // language=Cypher
            const string query = """
                                 MATCH (a:User { Email: $Email })
                                 RETURN a.Name as name, a.Email as email, a.Role as role, a.PasswordHash as passwordHash, a.PasswordSalt as passwordSalt, a.Id as id
                                 """;
            var cursor = await tx.RunAsync(query, new { loginDto.Email });
            var node = await cursor.SingleAsync();

            if (node is null)
                return null;

            var storedPasswordHash = node["passwordHash"].As<string>();
            var storedPasswordSalt = node["passwordSalt"].As<string>();

            using HMACSHA512 hmac = new(Convert.FromBase64String(storedPasswordSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            var storedHash = Convert.FromBase64String(storedPasswordHash);

            return computedHash.Where((t, i) => t != storedHash[i]).Any() ? null : GetUserFromNode(node);
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
                             MATCH (u:User { Id: $Id })
                             DETACH DELETE u
                             """;
        
        await tx.RunAsync(query, new { Id = userId.ToString() });
    }

    public async Task<bool> EmailExistsAsync(IAsyncQueryRunner tx, string email)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { Email: $Email })
                             WITH COUNT(u) > 0  as node_exists
                             RETURN node_exists
                             """;

        var accountCursor = await tx.RunAsync(query, new { Email = email });
        var result = await accountCursor.SingleAsync();
        return result["node_exists"].As<bool>();
    }
    
    private static User GetUserFromNode(IRecord node) =>
        new
        (
            Id: Guid.Parse(node["id"].As<string>()),
            Email: node["email"].As<string>(),
            Name: node["name"].As<string>(),
            Role: node["role"].As<string>()
        );
}
