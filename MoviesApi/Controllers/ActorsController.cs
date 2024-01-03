using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.Enums;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

public class ActorsController(IActorRepository actorRepository) : BaseApiController
{
    private IActorRepository ActorRepository { get; } = actorRepository;


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var actors = await ActorRepository.GetAllActors();
        return Ok(actors);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetActor(Guid id)
    {
        var actor = await ActorRepository.GetActor(id);

        return actor switch
        {
            null => NotFound($"Actor with id {id} was not found"),
            _ => Ok(actor)
        };
    }

    [HttpPost]
    public async Task<IActionResult> CreateActor(UpsertActorDto actorDto)
    {
        var newActor = await ActorRepository.CreateActor(actorDto);

        if (newActor is null)
            return BadRequest("Something went wrong when creating actor");

        return Ok(newActor);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateActor(Guid id, UpsertActorDto actorDto)
    {
        var updatedActor = await ActorRepository.UpdateActor(id, actorDto);

        return updatedActor switch
        {
            null => NotFound($"Actor with id {id} was not found"),
            _ => Ok(updatedActor)
        };
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteActor(Guid id)
    {
        var deleted = await ActorRepository.DeleteActor(id);

        return deleted switch
        {
            QueryResult.NotFound => NotFound($"Actor with id {id} was not found"),
            QueryResult.PhotoFailedToDelete => BadRequest("Something went wrong when deleting photo"),
            QueryResult.Completed => NoContent(),
            _ => throw new Exception("This shouldn't have happened")
        };
    }
}