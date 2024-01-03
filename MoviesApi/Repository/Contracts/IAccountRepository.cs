using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IAccountRepository
{
    Task<User?> RegisterAsync(RegisterDto registerDto);
    Task<User?> LoginAsync(LoginDto loginDto);
    Task<bool> EmailExistsAsync(string email);
}