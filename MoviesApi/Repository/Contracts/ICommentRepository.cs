using MoviesApi.DTOs;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface ICommentRepository
{
    Task<CommentDto?> GetCommentAsync(Guid commentId);
    Task<CommentDto?> AddCommentAsync(Guid userId, UpsertCommentDto upsertCommentDto);
    Task<CommentDto?> EditCommentAsync(Guid commentId, Guid userId, UpsertCommentDto upsertCommentDto);
    Task<QueryResult> DeleteCommentAsync(Guid commentId, Guid userId);
}