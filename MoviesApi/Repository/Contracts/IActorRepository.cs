using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors();
    Task<ActorDto?> GetActor(Guid id);
    Task<QueryResult<ActorDto>> CreateActor(UpsertActorDto actor);
    Task<QueryResult<ActorDto>> UpdateActor(Guid id, UpsertActorDto actor);
    Task<QueryResult> DeleteActor(Guid id);
}