using MoviesApi.Enums;

namespace MoviesApi.DTOs;

public record MemberDto(Guid Id, string Username, Role Role, DateTime LastActive);