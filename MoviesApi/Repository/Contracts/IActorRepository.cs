using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors();
    Task<ActorDto?> GetActor(Guid id);
    Task<ActorDto?> CreateActor(UpsertActorDto actor);
    Task<ActorDto?> UpdateActor(Guid id, UpsertActorDto actor);
    Task<QueryResult> DeleteActor(Guid id);
}