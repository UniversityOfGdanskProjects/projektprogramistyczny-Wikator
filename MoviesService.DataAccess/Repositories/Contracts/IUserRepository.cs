using MoviesService.Models;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(IAsyncQueryRunner tx);
    Task<int> GetUserActiveTodayCount(IAsyncQueryRunner tx);
    Task<User> UpdateUserNameAsync(IAsyncQueryRunner tx, Guid userId, string newUsername);
    Task ChangeUserRoleToAdminAsync(IAsyncQueryRunner tx, Guid userId);
    Task<bool> UserExistsAsync(IAsyncQueryRunner tx, Guid userId);
}