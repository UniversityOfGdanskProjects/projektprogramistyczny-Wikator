using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MoviesService.Models.Configurations;
using MoviesService.Services.Contracts;

namespace MoviesService.Services;

public class PhotoService(IOptions<CloudinarySettings> config) : IPhotoService
{
    private Cloudinary Cloudinary { get; } = new(new Account(
        config.Value.CloudName,
        config.Value.ApiKey,
        config.Value.ApiSecret
    ));

    public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file, string gravity = "auto")
    {
        var uploadResult = new ImageUploadResult();

        if (file.Length <= 0)
            return uploadResult;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation()
                .Height(1080)
                .Width(720)
                .Crop("fill")
                .AspectRatio(1.33)
                .Gravity(gravity),
            Folder = "p-bz-2"
        };

        return await Cloudinary.UploadAsync(uploadParams);
    }

    public async Task<DeletionResult> DeleteAsync(string publicId)
    {
        return await Cloudinary.DestroyAsync(new DeletionParams(publicId));
    }
}