using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Repository;

public class UserRepository(IDriver driver) : Repository(driver), IUserRepository
{
    public async Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(Guid? userId)
    {
        return await ExecuteReadAsync(async tx =>
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
                if (!Enum.TryParse<Role>(user["Role"].As<string>(), out var role))
                    throw new Exception("Role missing in User in Db");

                return new MemberDto(
                    Id: Guid.Parse(user["Id"].As<string>()),
                    Username: user["Name"].As<string>(),
                    Role: role,
                    LastActive: DateTime.Parse(user["LastActive"].As<string>())
                );
            });
        });
    }
}
