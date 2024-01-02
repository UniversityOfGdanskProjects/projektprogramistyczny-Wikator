using MoviesApi.DTOs;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class UserRepository(IDriver driver) : Repository(driver), IUserRepository
{
    public async Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(int? userId)
    {
        return await ExecuteAsync(async tx =>
        {
            const string query = """
                                 MATCH (u:User)
                                 WHERE $userId IS NULL OR ID(u) <> $userId
                                 RETURN ID(u) as Id, u
                                 ORDER BY u.LastActive DESC
                                 """;

            var result = await tx.RunAsync(query, new { userId });
            return await result.ToListAsync(record =>
            {
                var user = record["u"].As<INode>();
                if (!Enum.TryParse<Role>(user["Role"].As<string>(), out var role))
                    throw new Exception("Role missing in User in Db");

                return new MemberDto(
                    Id: record["Id"].As<int>(),
                    Username: user["Name"].As<string>(),
                    Role: role,
                    LastActive: DateTime.Parse(user["LastActive"].As<string>())
                );
            });
        });
    }
}
