namespace ExpenseHub.Api.Services;

/// <summary>
/// Fábrica de <see cref="ServiceResult{T}"/>.
/// </summary>
internal static class ServiceResult
{
    public static ServiceResult<T> Ok<T>(T value) => new(value, ServiceError.None, null);

    public static ServiceResult<T> Fail<T>(ServiceError error, string message) => new(default, error, message);
}
