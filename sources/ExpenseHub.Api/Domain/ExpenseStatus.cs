namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estados possíveis de um reembolso. Definidos exclusivamente pelo servidor.
/// </summary>
internal enum ExpenseStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Paid,
}
