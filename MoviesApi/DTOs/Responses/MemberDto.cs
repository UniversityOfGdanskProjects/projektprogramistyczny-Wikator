using MoviesApi.Enums;

namespace MoviesApi.DTOs.Responses;

public record MemberDto(Guid Id, string Username, Role Role, DateTime LastActive);