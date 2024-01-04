using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Helpers;

namespace MoviesApi.Repository.Contracts;

public interface ICommentRepository
{
    Task<CommentDto?> GetCommentAsync(Guid commentId);
    Task<QueryResult<CommentDto>> AddCommentAsync(Guid userId, AddCommentDto addCommentDto);
    Task<QueryResult<CommentDto>> EditCommentAsync(Guid commentId, Guid userId, EditCommentDto addCommentDto);
    Task<QueryResult> DeleteCommentAsync(Guid commentId, Guid userId);
}