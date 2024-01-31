using System.ComponentModel.DataAnnotations;

namespace MoviesService.Models.DTOs.Requests;

public class UpsertPictureDto
{
    [Required] public required byte[] FileContent { get; init; }
}