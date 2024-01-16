using MoviesApi.DTOs.Requests;
using MoviesApi.Models;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IAccountRepository
{
    Task<User?> RegisterAsync(IAsyncQueryRunner tx, RegisterDto registerDto);
    Task<User?> LoginAsync(IAsyncQueryRunner tx, LoginDto loginDto);
    Task DeleteUserAsync(IAsyncQueryRunner tx, Guid userId);
    Task<bool> EmailExistsAsync(IAsyncQueryRunner tx, string email);
}