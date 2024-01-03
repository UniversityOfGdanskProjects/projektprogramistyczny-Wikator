using MoviesApi.DTOs;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IIgnoresRepository
{
    Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(Guid userId);
    
    Task<QueryResult> IgnoreMovie(Guid userId, Guid movieId);
    Task<QueryResult> RemoveIgnoreMovie(Guid userId, Guid movieId);
}