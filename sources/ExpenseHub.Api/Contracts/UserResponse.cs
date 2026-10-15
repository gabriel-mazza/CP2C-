using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Usuário e suas roles.
/// </summary>
public sealed class UserResponse
{
    /// <summary>
    /// Inicializa a resposta.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="roles">Roles atribuídas ao usuário.</param>
    public UserResponse(string id, string? email, IReadOnlyList<string> roles)
    {
        Id = id;
        Email = email;
        Roles = roles;
    }

    /// <summary>
    /// Identificador do usuário.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// E-mail do usuário.
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Roles atribuídas ao usuário.
    /// </summary>
    public IReadOnlyList<string> Roles { get; }
}
