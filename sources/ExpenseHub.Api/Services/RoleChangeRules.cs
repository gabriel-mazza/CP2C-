using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Auth;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras para atribuição e remoção de roles pelo Admin.
/// </summary>
internal static class RoleChangeRules
{
    /// <summary>
    /// Normaliza as roles solicitadas: aceita apenas roles conhecidas (sem diferenciar maiúsculas),
    /// remove duplicadas e impede que o Admin retire a própria role Admin.
    /// </summary>
    /// <param name="actorId">Usuário que executa a alteração.</param>
    /// <param name="targetUserId">Usuário que terá as roles alteradas.</param>
    /// <param name="requestedRoles">Roles desejadas.</param>
    /// <returns>As roles normalizadas ou o erro de validação.</returns>
    public static ServiceResult<IReadOnlyList<string>> Normalize(
        string actorId,
        string targetUserId,
        IReadOnlyList<string>? requestedRoles)
    {
        if (requestedRoles is null)
        {
            return ServiceResult.Fail<IReadOnlyList<string>>(ServiceError.Validation, "A lista de roles é obrigatória.");
        }

        var normalized = new List<string>();
        foreach (string requested in requestedRoles)
        {
            string? known = Roles.All.FirstOrDefault(role => string.Equals(role, requested?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (known is null)
            {
                return ServiceResult.Fail<IReadOnlyList<string>>(ServiceError.Validation, $"Role desconhecida: '{requested}'.");
            }

            if (!normalized.Contains(known))
            {
                normalized.Add(known);
            }
        }

        bool removesOwnAdminRole = actorId == targetUserId && !normalized.Contains(Roles.Admin);
        if (removesOwnAdminRole)
        {
            return ServiceResult.Fail<IReadOnlyList<string>>(ServiceError.Validation, "O Admin não pode remover a própria role Admin.");
        }

        return ServiceResult.Ok<IReadOnlyList<string>>(normalized);
    }
}
