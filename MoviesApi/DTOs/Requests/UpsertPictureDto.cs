namespace MoviesApi.DTOs.Requests;

public class UpsertPictureDto
{
    public required byte[] FileContent { get; init; }
}