using System.Collections.Generic;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Nomes das roles reconhecidas pela aplicação.
/// </summary>
internal static class Roles
{
    public const string Admin = "Admin";

    public const string Employee = "Employee";

    public const string Approver = "Approver";

    public const string Finance = "Finance";

    public const string Auditor = "Auditor";

    /// <summary>
    /// Roles que podem consultar reembolsos (a visibilidade exata depende de cada role).
    /// </summary>
    public const string ExpenseReaders = Employee + "," + Approver + "," + Finance + "," + Auditor;

    public static IReadOnlyList<string> All { get; } = [Admin, Employee, Approver, Finance, Auditor];
}
