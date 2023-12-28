using MoviesApi.DTOs;
using MoviesApi.Models;

namespace MoviesApi.Repository;

public interface IAccountRepository
{
    Task<User?> RegisterAsync(LoginDto registerDto);
    Task<User?> LoginASync(LoginDto loginDto);
}