using System;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Converte entidades em DTOs; a API nunca expõe entidades diretamente.
/// </summary>
internal static class ExpenseMapper
{
    public static ExpenseResponse ToResponse(Expense expense) => new(
        expense.Id,
        expense.OwnerId,
        expense.Description,
        expense.Amount,
        expense.ExpenseDate,
        expense.CategoryId,
        expense.Status.ToString(),
        expense.RejectionReason,
        DateTime.SpecifyKind(expense.CreatedAtUtc, DateTimeKind.Utc),
        DateTime.SpecifyKind(expense.UpdatedAtUtc, DateTimeKind.Utc));

    public static ExpenseHistoryResponse ToHistoryResponse(ExpenseHistory history) => new(
        history.Id,
        history.ExpenseId,
        history.Action.ToString(),
        history.ActorId,
        DateTime.SpecifyKind(history.OccurredAtUtc, DateTimeKind.Utc),
        history.PreviousStatus?.ToString(),
        history.NewStatus.ToString(),
        history.Justification,
        history.Details);
}
