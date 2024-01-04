using MoviesApi.Enums;

namespace MoviesApi.Models;

public record User(Guid Id, string Name, string Email, Role Role);