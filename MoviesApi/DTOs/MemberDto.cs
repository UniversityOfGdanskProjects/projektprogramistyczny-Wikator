using MoviesApi.Enums;

namespace MoviesApi.DTOs;

public record MemberDto(int Id, string Username, Role Role, DateTime LastActive);