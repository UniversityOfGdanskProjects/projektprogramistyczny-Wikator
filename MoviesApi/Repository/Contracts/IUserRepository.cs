using MoviesApi.DTOs;
using MoviesApi.DTOs.Responses;

namespace MoviesApi.Repository.Contracts;

public interface IUserRepository
{
    Task<IEnumerable<MemberDto>> GetUsersByMostActiveAsync(Guid? userId);
}