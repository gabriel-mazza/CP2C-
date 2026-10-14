using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Registro imutável de uma ação executada sobre um reembolso.
/// </summary>
internal sealed class ExpenseHistory
{
    public int Id { get; set; }

    public Guid ExpenseId { get; set; }

    public ExpenseAction Action { get; set; }

    public string ActorId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public ExpenseStatus? PreviousStatus { get; set; }

    public ExpenseStatus NewStatus { get; set; }

    public string? Justification { get; set; }

    public string? Details { get; set; }
}
