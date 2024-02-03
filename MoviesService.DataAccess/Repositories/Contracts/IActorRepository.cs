using MoviesService.Models.DTOs.Requests;
using MoviesService.Models.DTOs.Responses;
using Neo4j.Driver;

namespace MoviesService.DataAccess.Repositories.Contracts;

public interface IActorRepository
{
    Task<IEnumerable<ActorDto>> GetAllActors(IAsyncQueryRunner tx);
    Task<ActorDto?> GetActor(IAsyncQueryRunner tx, Guid id);

    Task<ActorDto> CreateActor(IAsyncQueryRunner tx, AddActorDto actor, string? pictureAbsoluteUri,
        string? picturePublicId);

    Task<ActorDto> UpdateActor(IAsyncQueryRunner tx, Guid id, EditActorDto actor);
    Task DeleteActor(IAsyncQueryRunner tx, Guid id);
    Task AddActorPicture(IAsyncQueryRunner tx, Guid actorId, string pictureAbsoluteUri, string picturePublicId);
    Task DeleteActorPicture(IAsyncQueryRunner tx, Guid actorId);
    Task<bool> ActorExists(IAsyncQueryRunner tx, Guid id);
    Task<string?> GetPublicId(IAsyncQueryRunner tx, Guid actorId);
    Task<bool> ActorPictureExists(IAsyncQueryRunner tx, Guid actorId);
}