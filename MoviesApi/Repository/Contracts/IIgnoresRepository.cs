using MoviesApi.DTOs;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IIgnoresRepository
{
    Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(int userId);
    
    Task<QueryResult> IgnoreMovie(int userId, int movieId);
    Task<QueryResult> RemoveIgnoreMovie(int userId, int movieId);
}