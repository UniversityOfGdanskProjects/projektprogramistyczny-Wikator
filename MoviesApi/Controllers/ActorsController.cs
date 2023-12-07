using Microsoft.AspNetCore.Mvc;
using MoviesApi.DTOs;
using MoviesApi.Repository.Contracts;

namespace MoviesApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorsController(IActorRepository actorRepository) : ControllerBase
{
    private IActorRepository ActorRepository { get; } = actorRepository;


    [HttpPost]
    public async Task<IActionResult> CreateActor(AddActorDto actorDto)
    {
        var newActor = await ActorRepository.CreateActor(actorDto);

        if (newActor is null)
            return BadRequest("Something went wrong when creating actor");

        return Ok(newActor);
    }
}