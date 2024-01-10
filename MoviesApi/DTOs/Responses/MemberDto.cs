namespace MoviesApi.DTOs.Responses;

public record MemberDto(Guid Id, string Username, string Role, DateTime LastActive);