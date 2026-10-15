namespace ExpenseHub.Api.Services;

/// <summary>
/// Categorias de falha das operações de serviço, mapeadas para status HTTP na borda.
/// </summary>
public enum ServiceError
{
    /// <summary>
    /// Operação concluída sem erro.
    /// </summary>
    None,

    /// <summary>
    /// Entrada inválida (400).
    /// </summary>
    Validation,

    /// <summary>
    /// Credencial ausente ou inválida (401).
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Usuário sem permissão para a operação (403).
    /// </summary>
    Forbidden,

    /// <summary>
    /// Recurso inexistente ou fora do escopo de leitura (404).
    /// </summary>
    NotFound,

    /// <summary>
    /// Transição incompatível ou repetida (409).
    /// </summary>
    Conflict,
}
