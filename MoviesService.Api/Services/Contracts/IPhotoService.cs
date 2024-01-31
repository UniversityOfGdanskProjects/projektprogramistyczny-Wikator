using CloudinaryDotNet.Actions;

namespace MoviesService.Api.Services.Contracts;

public interface IPhotoService
{
    public Task<ImageUploadResult> AddPhotoAsync(IFormFile file, string gravity = "auto");
    public Task<DeletionResult> DeleteAsync(string publicId);
}