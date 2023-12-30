using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors();
    Task<ActorDto?> GetActor(int id);
    Task<ActorDto?> CreateActor(UpsertActorDto actor);
    Task<ActorDto?> UpdateActor(int id, UpsertActorDto actor);
    Task<bool> DeleteActor(int id);
}