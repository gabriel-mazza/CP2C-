using System;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Entrada do histórico de um reembolso.
/// </summary>
public sealed class ExpenseHistoryResponse
{
    /// <summary>
    /// Inicializa a resposta.
    /// </summary>
    /// <param name="id">Identificador da entrada.</param>
    /// <param name="expenseId">Reembolso ao qual a entrada pertence.</param>
    /// <param name="action">Ação executada.</param>
    /// <param name="actorId">Usuário que executou a ação.</param>
    /// <param name="occurredAtUtc">Instante da ação em UTC.</param>
    /// <param name="previousStatus">Estado anterior, quando existia.</param>
    /// <param name="newStatus">Estado posterior.</param>
    /// <param name="justification">Justificativa da reprovação, quando aplicável.</param>
    /// <param name="details">Alterações realizadas em rascunho, quando aplicável.</param>
    public ExpenseHistoryResponse(
        int id,
        Guid expenseId,
        string action,
        string actorId,
        DateTime occurredAtUtc,
        string? previousStatus,
        string newStatus,
        string? justification,
        string? details)
    {
        Id = id;
        ExpenseId = expenseId;
        Action = action;
        ActorId = actorId;
        OccurredAtUtc = occurredAtUtc;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Justification = justification;
        Details = details;
    }

    /// <summary>
    /// Identificador da entrada.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Reembolso ao qual a entrada pertence.
    /// </summary>
    public Guid ExpenseId { get; }

    /// <summary>
    /// Ação executada.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Usuário que executou a ação.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// Instante da ação em UTC (definido pelo servidor).
    /// </summary>
    public DateTime OccurredAtUtc { get; }

    /// <summary>
    /// Estado anterior, quando existia.
    /// </summary>
    public string? PreviousStatus { get; }

    /// <summary>
    /// Estado posterior.
    /// </summary>
    public string NewStatus { get; }

    /// <summary>
    /// Justificativa da reprovação, quando aplicável.
    /// </summary>
    public string? Justification { get; }

    /// <summary>
    /// Alterações realizadas em rascunho, quando aplicável.
    /// </summary>
    public string? Details { get; }
}
