namespace MoviesService.Models.DTOs.Responses;

public record MemberDto(Guid Id, string Username, string Role, DateTime LastActive);