using System;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Solicitação de reembolso de um funcionário.
/// </summary>
internal sealed class Expense
{
    public Guid Id { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public int? CategoryId { get; set; }

    public ExpenseStatus Status { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
