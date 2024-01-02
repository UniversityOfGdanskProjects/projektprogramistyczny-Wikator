using CloudinaryDotNet.Actions;

namespace MoviesApi.Services.Contracts;

public interface IPhotoService
{
    public Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
    public Task<DeletionResult> DeleteASync(string publicId);
}