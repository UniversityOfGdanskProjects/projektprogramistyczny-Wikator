namespace MoviesService.Models.DTOs.Responses;

public record CommentDto(
    Guid Id,
    Guid MovieId,
    Guid UserId,
    string Username,
    string Text,
    DateTime CreatedAt,
    bool IsEdited
);