using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs;
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
    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetActor(int id)
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
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateActor(int id, UpsertActorDto actorDto)
    {
        var updatedActor = await ActorRepository.UpdateActor(id, actorDto);

        return updatedActor switch
        {
            null => NotFound($"Actor with id {id} was not found"),
            _ => Ok(updatedActor)
        };
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteActor(int id)
    {
        var deleted = await ActorRepository.DeleteActor(id);

        return deleted switch
        {
            false => NotFound($"Actor with id {id} was not found"),
            true => NoContent()
        };
    }
}