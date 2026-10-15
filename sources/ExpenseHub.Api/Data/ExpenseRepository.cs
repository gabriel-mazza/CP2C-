using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Implementação de <see cref="IExpenseRepository"/> com Entity Framework Core.
/// </summary>
internal sealed class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Expenses.FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Expense>> ListAsync(
        Expression<Func<Expense, bool>> filter,
        CancellationToken cancellationToken)
    {
        // O filtro é aplicado no banco, antes de materializar os registros.
        return await _context.Expenses
            .AsNoTracking()
            .Where(filter)
            .OrderByDescending(expense => expense.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken)
    {
        return await _context.ExpenseHistories
            .AsNoTracking()
            .Where(history => history.ExpenseId == expenseId)
            .OrderBy(history => history.OccurredAtUtc)
            .ThenBy(history => history.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken) =>
        _context.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    public void Add(Expense expense) => _context.Expenses.Add(expense);

    public void AddHistory(ExpenseHistory history) => _context.ExpenseHistories.Add(history);

    public void AddPayment(PaymentRecord payment) => _context.Payments.Add(payment);

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Um único SaveChanges grava reembolso, histórico e pagamento na mesma transação.
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
