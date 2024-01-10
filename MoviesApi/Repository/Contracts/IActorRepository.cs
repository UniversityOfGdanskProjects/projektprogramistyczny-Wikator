using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors(IAsyncQueryRunner tx);
    Task<ActorDto?> GetActor(IAsyncQueryRunner tx, Guid id);
    Task<ActorDto> CreateActor(IAsyncQueryRunner tx, UpsertActorDto actor, string? pictureAbsoluteUri, string? picturePublicId);
    Task<ActorDto> UpdateActor(IAsyncQueryRunner tx, Guid id, UpsertActorDto actor);
    Task DeleteActor(IAsyncQueryRunner tx, Guid id);
    Task<bool> ActorExists(IAsyncQueryRunner tx, Guid id);
    Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid actorId);
}
