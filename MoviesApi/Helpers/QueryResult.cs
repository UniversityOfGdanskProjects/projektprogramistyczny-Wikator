using MoviesApi.Enums;

namespace MoviesApi.Helpers;

public record QueryResult<T>(QueryResultStatus Status, T? Data);

public record QueryResult(QueryResultStatus Status);
