namespace MoviesService.Models.DTOs.Responses;

public record ReviewDto(Guid Id, Guid UserId, Guid MovieId, int Score);