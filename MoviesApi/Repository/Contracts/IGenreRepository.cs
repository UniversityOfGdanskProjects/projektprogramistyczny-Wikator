using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IGenreRepository
{
    Task<List<string>> GetAllGenres(IAsyncQueryRunner tx);
}