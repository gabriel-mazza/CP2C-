using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;

namespace ExpenseHub.UnitTests.Support;

/// <summary>
/// Repositório em memória: os testes unitários não usam banco de dados nem rede.
/// </summary>
internal sealed class InMemoryExpenseRepository : IExpenseRepository
{
    private int _nextHistoryId = 1;

    public List<Expense> Expenses { get; } = [];

    public List<ExpenseHistory> History { get; } = [];

    public List<PaymentRecord> Payments { get; } = [];

    public HashSet<int> Categories { get; } = [1, 2];

    public bool SaveSucceeds { get; set; } = true;

    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Expenses.FirstOrDefault(expense => expense.Id == id));

    public Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken)
    {
        IReadOnlyList<Expense> result = Expenses.Where(filter.Compile()).ToList();

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExpenseHistory> result = History
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ThenBy(history => history.Id)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.Contains(categoryId));

    public void Add(Expense expense) => Expenses.Add(expense);

    public void AddHistory(ExpenseHistory history)
    {
        history.Id = _nextHistoryId++;
        History.Add(history);
    }

    public void AddPayment(PaymentRecord payment) => Payments.Add(payment);

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(SaveSucceeds);
}
