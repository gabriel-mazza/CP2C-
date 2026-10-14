namespace ExpenseHub.Api.Domain;

/// <summary>
/// Ações registradas no histórico de um reembolso.
/// </summary>
internal enum ExpenseAction
{
    Created,
    Edited,
    Submitted,
    Approved,
    Rejected,
    Paid,
}
