using MoviesApi.DTOs;
using MoviesApi.DTOs.Requests;
using MoviesApi.DTOs.Responses;
using MoviesApi.Enums;

namespace MoviesApi.Repository.Contracts;

public interface ICommentRepository
{
    Task<CommentDto?> GetCommentAsync(Guid commentId);
    Task<CommentDto?> AddCommentAsync(Guid userId, AddCommentDto addCommentDto);
    Task<CommentDto?> EditCommentAsync(Guid commentId, Guid userId, EditCommentDto addCommentDto);
    Task<QueryResult> DeleteCommentAsync(Guid commentId, Guid userId);
}