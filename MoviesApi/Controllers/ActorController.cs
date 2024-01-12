using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.Controllers.Base;
using MoviesApi.DTOs.Requests;
using MoviesApi.Exceptions;
using MoviesApi.Repository.Contracts;
using MoviesApi.Services.Contracts;
using Neo4j.Driver;

namespace MoviesApi.Controllers;

[Route("api/[controller]")]
[Authorize(Policy = "RequireAdminRole")]
public class ActorController(IDriver driver, IPhotoService photoService, IActorRepository actorRepository)
    : BaseApiController(driver)
{
    private IPhotoService PhotoService { get; } = photoService;
    private IActorRepository ActorRepository { get; } = actorRepository;


    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        return await ExecuteReadAsync(async tx =>
        {
            var actors = await ActorRepository.GetAllActors(tx);
            return Ok(actors);
        });
    }
    
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActor(Guid id)
    {
        return await ExecuteReadAsync<IActionResult>(async tx =>
        {
            var actor = await ActorRepository.GetActor(tx, id);

            return actor switch
            {
                null => NotFound($"Actor with id {id} was not found"),
                _ => Ok(actor)
            };
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateActor(AddActorDto actorDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            string? pictureAbsoluteUri = null;
            string? picturePublicId = null;
		
            if (actorDto.FileContent is not null)
            {
                var file = new FormFile(
                    new MemoryStream(actorDto.FileContent),
                    0,
                    actorDto.FileContent.Length,
                    "file", actorDto.FileName ?? $"movie-{new Guid()}"
                    );

                var uploadResult = await PhotoService.AddPhotoAsync(file, Gravity.Face);
                if (uploadResult.Error is not null)
                    throw new PhotoServiceException("Photo failed to save, please try again in few minutes");

                pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
                picturePublicId = uploadResult.PublicId;
            }
            
            var actor = await ActorRepository.CreateActor(tx, actorDto, pictureAbsoluteUri, picturePublicId);
            return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, actor);
        });
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateActor(Guid id, UpdateActorDto actorDto)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await ActorRepository.ActorExists(tx, id))
                return NotFound($"Actor with id {id} was not found");
            
            string? pictureAbsoluteUri = null;
            string? picturePublicId = null;

            if (actorDto.FileContent is not null)
            {
                var publicId = await ActorRepository.GetPublicId(tx, id);
                if (publicId is not null)
                {
                    var deletionResult = await PhotoService.DeleteAsync(publicId);
                    if (deletionResult.Error is not null)
                        throw new PhotoServiceException("Failed to delete photo, please try again in few minutes");
                }
                
                var file = new FormFile(
                    new MemoryStream(actorDto.FileContent),
                    0,
                    actorDto.FileContent.Length,
                    "file", actorDto.FileName ?? $"movie-{new Guid()}"
                );

                var uploadResult = await PhotoService.AddPhotoAsync(file);
                if (uploadResult.Error is not null)
                    throw new PhotoServiceException("Photo failed to save, please try again in few minutes");

                pictureAbsoluteUri = uploadResult.SecureUrl.AbsoluteUri;
                picturePublicId = uploadResult.PublicId;
            }
            
            var actor = await ActorRepository.UpdateActor(tx, id, actorDto, pictureAbsoluteUri, picturePublicId);
            return Ok(actor);
        });
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteActor(Guid id)
    {
        return await ExecuteWriteAsync<IActionResult>(async tx =>
        {
            if (!await ActorRepository.ActorExists(tx, id))
                return NotFound($"Actor with id {id} was not found");
            
            var publicId = await ActorRepository.GetPublicId(tx, id);
            if (publicId is not null)
            {
                var deletionResult = await PhotoService.DeleteAsync(publicId);
                if (deletionResult.Error is not null)
                    throw new PhotoServiceException("Failed to delete photo, please try again in few minutes");
            }
            
            await ActorRepository.DeleteActor(tx, id);
            return NoContent();
        });
    }
}
