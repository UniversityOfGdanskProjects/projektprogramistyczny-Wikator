namespace MoviesService.Models.DTOs.Responses;

public record UserDto(Guid Id, string Name, string Role, string Token);