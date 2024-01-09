namespace MoviesApi.DTOs.Responses;

public record UserDto(Guid Id ,string Name, string Role, string Token);
