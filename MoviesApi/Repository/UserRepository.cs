﻿using MoviesApi.DTOs.Responses;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class UserRepository : IUserRepository
{
    public async Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(IAsyncQueryRunner tx, Guid? userId)
    {
        // language=Cypher
        const string query = """
                             MATCH (u:User)
                             WHERE $userId IS NULL OR u.Id <> $userId
                             RETURN u
                             ORDER BY u.LastActive DESC
                             """;

        var result = await tx.RunAsync(query, new { userId = userId.ToString() });
        return await result.ToListAsync(record =>
        {
            var user = record["u"].As<INode>();

            return new MemberDto(
                Id: Guid.Parse(user["Id"].As<string>()),
                Username: user["Name"].As<string>(),
                Role: user["Role"].As<string>(),
                LastActive: DateTime.Parse(user["LastActive"].As<string>())
            );
        });
    }
}
