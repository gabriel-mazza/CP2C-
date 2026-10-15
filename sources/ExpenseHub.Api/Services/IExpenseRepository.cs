using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Acesso a dados dos reembolsos. Alterações ficam pendentes até <see cref="SaveChangesAsync"/>,
/// que as persiste em uma única operação lógica.
/// </summary>
internal interface IExpenseRepository
{
    Task<Expense?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExpenseHistory>> GetHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    void Add(Expense expense);

    void AddHistory(ExpenseHistory history);

    void AddPayment(PaymentRecord payment);

    /// <summary>
    /// Persiste todas as alterações pendentes de forma atômica.
    /// </summary>
    /// <returns>Falso quando outra requisição alterou o reembolso ao mesmo tempo.</returns>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken);
}
