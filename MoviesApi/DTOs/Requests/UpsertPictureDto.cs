using System.ComponentModel.DataAnnotations;

namespace MoviesApi.DTOs.Requests;

public class UpsertPictureDto
{
    [Required] public required byte[] FileContent { get; init; }
}