using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Define quais reembolsos cada perfil pode ler. As roles acumulam (união das permissões).
/// </summary>
internal static class ExpenseVisibility
{
    /// <summary>
    /// Employee vê os próprios; Approver vê os enviados; Finance vê aprovados e pagos;
    /// Auditor vê todos. Admin, por si só, não vê reembolsos.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <returns>Filtro que pode ser traduzido para SQL, evitando materializar registros fora do escopo.</returns>
    public static Expression<Func<Expense, bool>> For(ActorContext actor)
    {
        bool seesAll = actor.IsInRole(Roles.Auditor);
        string? ownerId = actor.IsInRole(Roles.Employee) ? actor.UserId : null;
        bool seesSubmitted = actor.IsInRole(Roles.Approver);
        bool seesApprovedAndPaid = actor.IsInRole(Roles.Finance);

        return expense => seesAll
            || (ownerId != null && expense.OwnerId == ownerId)
            || (seesSubmitted && expense.Status == ExpenseStatus.Submitted)
            || (seesApprovedAndPaid && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }

    /// <summary>
    /// Indica se o usuário possui ao menos uma role que permite consultar reembolsos.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <returns>Verdadeiro quando o usuário pode ler reembolsos.</returns>
    public static bool CanRead(ActorContext actor) =>
        actor.IsInRole(Roles.Employee)
        || actor.IsInRole(Roles.Approver)
        || actor.IsInRole(Roles.Finance)
        || actor.IsInRole(Roles.Auditor);
}
