using MoviesService.Models;
using MoviesService.Models.DTOs.Requests;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IAccountRepository
{
    Task<User> RegisterAsync(IAsyncQueryRunner tx, RegisterDto registerDto);
    Task<User?> LoginAsync(IAsyncQueryRunner tx, LoginDto loginDto);
    Task DeleteUserAsync(IAsyncQueryRunner tx, Guid userId);
    Task<bool> EmailExistsAsync(IAsyncQueryRunner tx, string email);
}