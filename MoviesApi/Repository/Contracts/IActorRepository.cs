using MoviesApi.DTOs;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors();
    Task<ActorDto?> GetActor(int id);
    Task<ActorDto?> CreateActor(UpsertActorDto actor);
    Task<ActorDto?> UpdateActor(int id, UpsertActorDto actor);
    Task<QueryResult> DeleteActor(int id);
}