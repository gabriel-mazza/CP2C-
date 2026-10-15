using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Identidade autenticada que executa uma operação. É sempre derivada do token,
/// nunca de dados enviados pelo cliente.
/// </summary>
public sealed class ActorContext
{
    private readonly HashSet<string> _roleNames;

    /// <summary>
    /// Inicializa um contexto de ator.
    /// </summary>
    /// <param name="userId">Identificador do usuário autenticado.</param>
    /// <param name="roleNames">Roles do usuário.</param>
    public ActorContext(string userId, IEnumerable<string> roleNames)
    {
        ArgumentNullException.ThrowIfNull(roleNames);

        UserId = userId;
        _roleNames = new HashSet<string>(roleNames, StringComparer.Ordinal);
    }

    /// <summary>
    /// Identificador do usuário autenticado.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Roles do usuário autenticado.
    /// </summary>
    public IReadOnlyCollection<string> RoleNames => _roleNames;

    /// <summary>
    /// Cria o contexto a partir das claims do token.
    /// </summary>
    /// <param name="principal">Usuário autenticado da requisição.</param>
    /// <returns>O contexto do ator.</returns>
    public static ActorContext FromPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        IEnumerable<string> roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

        return new ActorContext(userId, roles);
    }

    /// <summary>
    /// Indica se o usuário possui a role informada.
    /// </summary>
    /// <param name="role">Nome da role.</param>
    /// <returns>Verdadeiro quando o usuário possui a role.</returns>
    public bool IsInRole(string role) => _roleNames.Contains(role);
}
