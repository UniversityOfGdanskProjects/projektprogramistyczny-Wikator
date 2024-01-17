using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(IAsyncQueryRunner tx, Guid? userId);
    Task<bool> UserExistsAsync(IAsyncQueryRunner tx, Guid userId);
}