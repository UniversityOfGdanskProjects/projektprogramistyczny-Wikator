using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
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

        return newActor.Status switch
        {
            QueryResultStatus.PhotoFailedToSave => BadRequest("Photo failed to save, please try again in few minutes"),
            QueryResultStatus.Completed => CreatedAtAction(nameof(GetActor), new { id = newActor.Data!.Id },
                newActor.Data),
            _ => throw new Exception("Unexpected status returned")
        };
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateActor(Guid id, UpsertActorDto actorDto)
    {
        var updatedActor = await ActorRepository.UpdateActor(id, actorDto);

        return updatedActor.Status switch
        {
            QueryResultStatus.NotFound => NotFound($"Actor with id {id} was not found"),
            QueryResultStatus.Completed => Ok(updatedActor.Data),
            _ => throw new Exception("Unexpected status returned")
        };
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteActor(Guid id)
    {
        var deleted = await ActorRepository.DeleteActor(id);

        return deleted.Status switch
        {
            QueryResultStatus.NotFound => NotFound($"Actor with id {id} was not found"),
            QueryResultStatus.PhotoFailedToDelete => BadRequest("Failed to delete photo, please try again in few minutes"),
            QueryResultStatus.Completed => NoContent(),
            _ => throw new Exception("Unexpected status returned")
        };
    }
}
