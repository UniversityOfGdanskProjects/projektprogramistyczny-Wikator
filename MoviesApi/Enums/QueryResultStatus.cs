namespace MoviesApi.Enums;

public enum QueryResultStatus
{
    Completed,
    NotFound,
    RelatedEntityDoesNotExists,
    EntityAlreadyExists,
    RelationDoesNotExist,
    UnexpectedError,
    PhotoFailedToDelete,
    PhotoFailedToSave
}