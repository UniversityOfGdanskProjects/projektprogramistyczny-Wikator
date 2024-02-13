using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IIgnoresRepository
{
    Task<IEnumerable<MovieDto>> GetAllIgnoreMovies(IAsyncQueryRunner tx, Guid userId);

    Task IgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task RemoveIgnoreMovie(IAsyncQueryRunner tx, Guid userId, Guid movieId);
    Task<bool> IgnoresExists(IAsyncQueryRunner tx, Guid movieId, Guid userId);
}