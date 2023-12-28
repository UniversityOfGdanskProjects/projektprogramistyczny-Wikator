using MoviesApi.DTOs;
using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IAccountRepository
{
    Task<User?> RegisterAsync(RegisterDto registerDto);
    Task<User?> LoginASync(LoginDto loginDto);
    Task<bool> EmailExistsAsync(string email);
}