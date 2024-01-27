using MoviesApi.DTOs.Responses;
using MoviesApi.Models;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(IAsyncQueryRunner tx);
    Task<User> UpdateUserNameAsync(IAsyncQueryRunner tx, Guid userId, string newUsername);
    Task ChangeUserRoleToAdminAsync(IAsyncQueryRunner tx, Guid userId);
    Task<bool> UserExistsAsync(IAsyncQueryRunner tx, Guid userId);
}