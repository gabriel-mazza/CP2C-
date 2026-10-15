namespace ExpenseHub.Api.Services;

/// <summary>
/// Resultado de uma operação de serviço: um valor ou um erro categorizado.
/// </summary>
/// <typeparam name="T">Tipo do valor retornado em caso de sucesso.</typeparam>
public sealed class ServiceResult<T>
{
    internal ServiceResult(T? value, ServiceError error, string? message)
    {
        Value = value;
        Error = error;
        Message = message;
    }

    /// <summary>
    /// Valor retornado quando a operação teve sucesso.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Categoria do erro, ou <see cref="ServiceError.None"/> em caso de sucesso.
    /// </summary>
    public ServiceError Error { get; }

    /// <summary>
    /// Mensagem descritiva do erro.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Indica se a operação teve sucesso.
    /// </summary>
    public bool IsSuccess => Error == ServiceError.None;
}
