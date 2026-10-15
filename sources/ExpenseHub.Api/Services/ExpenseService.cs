using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Implementa o fluxo de reembolsos: Draft, Submitted, Approved ou Rejected, e Paid.
/// Toda decisão de acesso combina role, ownership, estado e escopo de leitura.
/// </summary>
internal sealed class ExpenseService : IExpenseService
{
    private const string ForbiddenRoleMessage = "O perfil do usuário não permite esta operação.";
    private const string NotFoundMessage = "Reembolso não encontrado.";
    private const string InvalidStateMessage = "O reembolso não está no estado esperado para esta operação.";
    private const string ConcurrencyMessage = "O reembolso foi alterado por outra requisição. Tente novamente.";

    private static readonly WorkflowStep _approveStep =
        new(Roles.Approver, ExpenseStatus.Submitted, ExpenseStatus.Approved, ExpenseAction.Approved);

    private static readonly WorkflowStep _rejectStep =
        new(Roles.Approver, ExpenseStatus.Submitted, ExpenseStatus.Rejected, ExpenseAction.Rejected);

    private static readonly WorkflowStep _payStep =
        new(Roles.Finance, ExpenseStatus.Approved, ExpenseStatus.Paid, ExpenseAction.Paid);

    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(
        ActorContext actor,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Employee))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        ServiceResult<ExpenseInput> parsed = await ParseInputAsync(request, now, cancellationToken);
        if (parsed.Value is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(parsed.Error, parsed.Message ?? string.Empty);
        }

        ExpenseInput input = parsed.Value;
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            OwnerId = actor.UserId,
            Description = input.Description,
            Amount = input.Amount,
            ExpenseDate = input.ExpenseDate,
            CategoryId = input.CategoryId,
            Status = ExpenseStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        string details = string.Create(
            CultureInfo.InvariantCulture,
            $"Rascunho criado: valor {input.Amount}, data {input.ExpenseDate:yyyy-MM-dd}");

        _repository.Add(expense);
        _repository.AddHistory(NewHistory(expense.Id, ExpenseAction.Created, actor, now, null, ExpenseStatus.Draft, null, details));

        return await SaveAsync(expense, cancellationToken);
    }

    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        ActorContext actor,
        Guid id,
        SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Employee))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        Expense? expense = await FindVisibleAsync(actor, id, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.NotFound, NotFoundMessage);
        }

        if (expense.OwnerId != actor.UserId)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, "Somente o proprietário pode editar o reembolso.");
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Conflict, "Somente rascunhos podem ser editados.");
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        ServiceResult<ExpenseInput> parsed = await ParseInputAsync(request, now, cancellationToken);
        if (parsed.Value is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(parsed.Error, parsed.Message ?? string.Empty);
        }

        ExpenseInput input = parsed.Value;
        string? changes = DescribeChanges(expense, input);
        if (changes is null)
        {
            return ServiceResult.Ok(ExpenseMapper.ToResponse(expense));
        }

        expense.Description = input.Description;
        expense.Amount = input.Amount;
        expense.ExpenseDate = input.ExpenseDate;
        expense.CategoryId = input.CategoryId;
        expense.UpdatedAtUtc = now;

        _repository.AddHistory(NewHistory(expense.Id, ExpenseAction.Edited, actor, now, ExpenseStatus.Draft, ExpenseStatus.Draft, null, changes));

        return await SaveAsync(expense, cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!ExpenseVisibility.CanRead(actor))
        {
            return ServiceResult.Fail<IReadOnlyList<ExpenseResponse>>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        IReadOnlyList<Expense> expenses = await _repository.ListAsync(ExpenseVisibility.For(actor), cancellationToken);
        IReadOnlyList<ExpenseResponse> response = expenses.Select(ExpenseMapper.ToResponse).ToList();

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult<ExpenseResponse>> GetAsync(
        ActorContext actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!ExpenseVisibility.CanRead(actor))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        Expense? expense = await FindVisibleAsync(actor, id, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.NotFound, NotFoundMessage);
        }

        return ServiceResult.Ok(ExpenseMapper.ToResponse(expense));
    }

    public async Task<ServiceResult<ExpenseResponse>> SubmitAsync(
        ActorContext actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Employee))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        Expense? expense = await FindVisibleAsync(actor, id, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.NotFound, NotFoundMessage);
        }

        if (expense.OwnerId != actor.UserId)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, "Somente o proprietário pode enviar o reembolso.");
        }

        if (expense.Status != ExpenseStatus.Draft)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Conflict, InvalidStateMessage);
        }

        ApplyTransition(expense, ExpenseAction.Submitted, ExpenseStatus.Submitted, actor, _timeProvider.GetUtcNow().UtcDateTime, null);

        return await SaveAsync(expense, cancellationToken);
    }

    public Task<ServiceResult<ExpenseResponse>> ApproveAsync(
        ActorContext actor,
        Guid id,
        CancellationToken cancellationToken) =>
        ApplyWorkflowAsync(actor, id, _approveStep, null, cancellationToken);

    public async Task<ServiceResult<ExpenseResponse>> RejectAsync(
        ActorContext actor,
        Guid id,
        RejectExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Approver))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        string? justification = request.Justification?.Trim();
        string? error = ExpenseRules.ValidateJustification(justification);
        if (error is not null)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Validation, error);
        }

        return await ApplyWorkflowAsync(actor, id, _rejectStep, justification, cancellationToken);
    }

    public Task<ServiceResult<ExpenseResponse>> PayAsync(
        ActorContext actor,
        Guid id,
        CancellationToken cancellationToken) =>
        ApplyWorkflowAsync(actor, id, _payStep, null, cancellationToken);

    public async Task<ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>> GetHistoryAsync(
        ActorContext actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!ExpenseVisibility.CanRead(actor))
        {
            return ServiceResult.Fail<IReadOnlyList<ExpenseHistoryResponse>>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        Expense? expense = await FindVisibleAsync(actor, id, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Fail<IReadOnlyList<ExpenseHistoryResponse>>(ServiceError.NotFound, NotFoundMessage);
        }

        IReadOnlyList<ExpenseHistory> history = await _repository.GetHistoryAsync(expense.Id, cancellationToken);
        IReadOnlyList<ExpenseHistoryResponse> response = history.Select(ExpenseMapper.ToHistoryResponse).ToList();

        return ServiceResult.Ok(response);
    }

    private static ExpenseHistory NewHistory(
        Guid expenseId,
        ExpenseAction action,
        ActorContext actor,
        DateTime occurredAtUtc,
        ExpenseStatus? previousStatus,
        ExpenseStatus newStatus,
        string? justification,
        string? details) => new()
        {
            ExpenseId = expenseId,
            Action = action,
            ActorId = actor.UserId,
            OccurredAtUtc = occurredAtUtc,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Justification = justification,
            Details = details,
        };

    private static string? DescribeChanges(Expense current, ExpenseInput input)
    {
        var changes = new List<string>();

        if (!string.Equals(current.Description, input.Description, StringComparison.Ordinal))
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"Descrição: '{current.Description}' -> '{input.Description}'"));
        }

        if (current.Amount != input.Amount)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"Valor: {current.Amount} -> {input.Amount}"));
        }

        if (current.ExpenseDate != input.ExpenseDate)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"Data: {current.ExpenseDate:yyyy-MM-dd} -> {input.ExpenseDate:yyyy-MM-dd}"));
        }

        if (current.CategoryId != input.CategoryId)
        {
            changes.Add(string.Create(CultureInfo.InvariantCulture, $"Categoria: {current.CategoryId} -> {input.CategoryId}"));
        }

        return changes.Count == 0 ? null : string.Join("; ", changes);
    }

    private async Task<ServiceResult<ExpenseInput>> ParseInputAsync(
        SaveExpenseRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string? description = request.Description?.Trim();
        DateOnly? expenseDate = request.ExpenseDate is null ? null : DateOnly.FromDateTime(request.ExpenseDate.Value);

        string? error = ExpenseRules.ValidateExpense(description, request.Amount, expenseDate, DateOnly.FromDateTime(now));
        if (error is not null)
        {
            return ServiceResult.Fail<ExpenseInput>(ServiceError.Validation, error);
        }

        if (request.CategoryId is int categoryId && !await _repository.CategoryExistsAsync(categoryId, cancellationToken))
        {
            return ServiceResult.Fail<ExpenseInput>(ServiceError.Validation, "A categoria informada não existe.");
        }

        var input = new ExpenseInput(
            description ?? string.Empty,
            request.Amount.GetValueOrDefault(),
            expenseDate.GetValueOrDefault(),
            request.CategoryId);

        return ServiceResult.Ok(input);
    }

    private async Task<Expense?> FindVisibleAsync(ActorContext actor, Guid id, CancellationToken cancellationToken)
    {
        Expense? expense = await _repository.FindAsync(id, cancellationToken);
        if (expense is null || !ExpenseVisibility.For(actor).Compile().Invoke(expense))
        {
            return null;
        }

        return expense;
    }

    private async Task<ServiceResult<ExpenseResponse>> ApplyWorkflowAsync(
        ActorContext actor,
        Guid id,
        WorkflowStep step,
        string? justification,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(step.RequiredRole))
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, ForbiddenRoleMessage);
        }

        Expense? expense = await _repository.FindAsync(id, cancellationToken);
        if (expense is null)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.NotFound, NotFoundMessage);
        }

        if (expense.OwnerId == actor.UserId)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Forbidden, "O proprietário não pode aprovar, reprovar ou pagar o próprio reembolso.");
        }

        if (expense.Status != step.From)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Conflict, InvalidStateMessage);
        }

        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        ApplyTransition(expense, step.Action, step.To, actor, now, justification);

        if (step.Action == ExpenseAction.Rejected)
        {
            expense.RejectionReason = justification;
        }

        if (step.Action == ExpenseAction.Paid)
        {
            _repository.AddPayment(new PaymentRecord
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                PaidByUserId = actor.UserId,
                Amount = expense.Amount,
                PaidAtUtc = now,
            });
        }

        return await SaveAsync(expense, cancellationToken);
    }

    private void ApplyTransition(
        Expense expense,
        ExpenseAction action,
        ExpenseStatus newStatus,
        ActorContext actor,
        DateTime now,
        string? justification)
    {
        ExpenseStatus previousStatus = expense.Status;
        expense.Status = newStatus;
        expense.UpdatedAtUtc = now;

        _repository.AddHistory(NewHistory(expense.Id, action, actor, now, previousStatus, newStatus, justification, null));
    }

    private async Task<ServiceResult<ExpenseResponse>> SaveAsync(Expense expense, CancellationToken cancellationToken)
    {
        bool saved = await _repository.SaveChangesAsync(cancellationToken);
        if (!saved)
        {
            return ServiceResult.Fail<ExpenseResponse>(ServiceError.Conflict, ConcurrencyMessage);
        }

        return ServiceResult.Ok(ExpenseMapper.ToResponse(expense));
    }

    private sealed class ExpenseInput
    {
        public ExpenseInput(string description, decimal amount, DateOnly expenseDate, int? categoryId)
        {
            Description = description;
            Amount = amount;
            ExpenseDate = expenseDate;
            CategoryId = categoryId;
        }

        public string Description { get; }

        public decimal Amount { get; }

        public DateOnly ExpenseDate { get; }

        public int? CategoryId { get; }
    }

    private sealed class WorkflowStep
    {
        public WorkflowStep(string requiredRole, ExpenseStatus from, ExpenseStatus to, ExpenseAction action)
        {
            RequiredRole = requiredRole;
            From = from;
            To = to;
            Action = action;
        }

        public string RequiredRole { get; }

        public ExpenseStatus From { get; }

        public ExpenseStatus To { get; }

        public ExpenseAction Action { get; }
    }
}
