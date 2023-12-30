using MoviesApi.DTOs;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<ActorDto?> CreateActor(AddActorDto actor);
}