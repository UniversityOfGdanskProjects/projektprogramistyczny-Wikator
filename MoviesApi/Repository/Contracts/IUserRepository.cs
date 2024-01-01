using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(int? userId);
}