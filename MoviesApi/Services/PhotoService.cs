using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using MoviesApi.Configurations;
using MoviesApi.Services.Contracts;

namespace MoviesApi.Services;

public class PhotoService(IOptions<CloudinarySettings> config) : IPhotoService
{
    private Cloudinary Cloudinary { get; } = new(new Account(
        config.Value.CloudName,
        config.Value.ApiKey,
        config.Value.ApiSecret
    ));

    public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
    {
        var uploadResult = new ImageUploadResult();

        if (file.Length <= 0)
            return uploadResult;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation()
                .Height(500)
                .Width(750)
                .Crop("fill")
                .Gravity(Gravity.Auto),
            Folder = "p-bz-2"
        };

        uploadResult = await Cloudinary.UploadAsync(uploadParams);

        return uploadResult;
    }
}