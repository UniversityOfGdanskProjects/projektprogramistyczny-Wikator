using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IGenreRepository
{
    Task<List<string>> GetAllGenres(IAsyncQueryRunner tx);
}