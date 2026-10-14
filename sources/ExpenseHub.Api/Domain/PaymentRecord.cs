using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Pagamento simulado de um reembolso aprovado.
/// </summary>
internal sealed class PaymentRecord
{
    public Guid Id { get; set; }

    public Guid ExpenseId { get; set; }

    public string PaidByUserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime PaidAtUtc { get; set; }
}
