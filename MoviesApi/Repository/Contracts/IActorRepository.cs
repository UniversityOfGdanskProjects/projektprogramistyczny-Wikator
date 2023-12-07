using MoviesApi.DTOs;
using MoviesApi.Models;

namespace MoviesApi.Repository.Contracts;

public interface IActorRepository
{
    Task<Actor?> CreateActor(AddActorDto actor);
}