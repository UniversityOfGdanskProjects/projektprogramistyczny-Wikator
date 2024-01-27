using MoviesApi.DTOs.Responses;
using MoviesApi.Extensions;
using MoviesApi.Models;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class UserRepository : IUserRepository
{
    public async Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(IAsyncQueryRunner tx)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User)
                             RETURN u
                             ORDER BY u.activityScore DESC
                             """;

        var result = await tx.RunAsync(query);
        return await result.ToListAsync(record =>
        {
            var user = record["u"].As<INode>();

            return new MemberDto(
                Id: Guid.Parse(user["id"].As<string>()),
                Username: user["name"].As<string>(),
                Role: user["role"].As<string>(),
                LastActive: DateTime.Parse(user["lastActive"].As<string>())
            );
        });
    }

    public async Task<User> UpdateUserNameAsync(IAsyncQueryRunner tx, Guid userId, string newUsername)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             SET u.name = $newUsername
                             RETURN 
                               u.id AS id,
                               u.name AS name,
                               u.email as email,
                               u.role AS role
                             """;
        
        var cursor = await tx.RunAsync(query, new { userId = userId.ToString(), newUsername });
        return await cursor.SingleAsync(record => record.ConvertToUser());
    }

    public async Task ChangeUserRoleToAdminAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             SET u.role = 'Admin'
                             """;
        
        await tx.RunAsync(query, new { userId = userId.ToString() });
    }

    public async Task<bool> UserExistsAsync(IAsyncQueryRunner tx, Guid userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User { id: $userId })
                             RETURN COUNT(u) > 0 AS userExists
                             """;

        var cursor = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await cursor.SingleAsync(record => record["userExists"].As<bool>());
    }
}
