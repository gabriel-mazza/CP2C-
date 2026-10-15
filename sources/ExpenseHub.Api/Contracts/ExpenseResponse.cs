using System;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Representação de um reembolso retornada pela API.
/// </summary>
public sealed class ExpenseResponse
{
    /// <summary>
    /// Inicializa a resposta.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="ownerId">Identificador do proprietário.</param>
    /// <param name="description">Descrição da despesa.</param>
    /// <param name="amount">Valor da despesa.</param>
    /// <param name="expenseDate">Data da despesa.</param>
    /// <param name="categoryId">Categoria, quando informada.</param>
    /// <param name="status">Estado atual.</param>
    /// <param name="rejectionReason">Justificativa, quando reprovado.</param>
    /// <param name="createdAtUtc">Instante de criação em UTC.</param>
    /// <param name="updatedAtUtc">Instante da última alteração em UTC.</param>
    public ExpenseResponse(
        Guid id,
        string ownerId,
        string description,
        decimal amount,
        DateOnly expenseDate,
        int? categoryId,
        string status,
        string? rejectionReason,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        Status = status;
        RejectionReason = rejectionReason;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Identificador do reembolso.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Identificador do proprietário.
    /// </summary>
    public string OwnerId { get; }

    /// <summary>
    /// Descrição da despesa.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Valor da despesa.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Data da despesa.
    /// </summary>
    public DateOnly ExpenseDate { get; }

    /// <summary>
    /// Categoria, quando informada.
    /// </summary>
    public int? CategoryId { get; }

    /// <summary>
    /// Estado atual (Draft, Submitted, Approved, Rejected ou Paid).
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Justificativa, quando reprovado.
    /// </summary>
    public string? RejectionReason { get; }

    /// <summary>
    /// Instante de criação em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Instante da última alteração em UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; }
}
