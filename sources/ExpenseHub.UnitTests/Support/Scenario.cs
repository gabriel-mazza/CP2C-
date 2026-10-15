using System;
using System.Threading;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;

namespace ExpenseHub.UnitTests.Support;

/// <summary>
/// Monta um serviço isolado (repositório e relógio próprios) para cada teste.
/// </summary>
internal sealed class Scenario
{
    public Scenario()
    {
        Repository = new InMemoryExpenseRepository();
        Clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
        Service = new ExpenseService(Repository, Clock);
    }

    public InMemoryExpenseRepository Repository { get; }

    public FixedTimeProvider Clock { get; }

    public ExpenseService Service { get; }

    public static CancellationToken Token => CancellationToken.None;

    public static SaveExpenseRequest ValidRequest(Action<SaveExpenseRequest>? customize = null)
    {
        var request = new SaveExpenseRequest
        {
            Description = "Almoço com cliente",
            Amount = 50m,
            ExpenseDate = new DateTime(2026, 10, 10),
        };

        customize?.Invoke(request);

        return request;
    }

    public Expense Seed(string ownerId, ExpenseStatus status)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Description = "Despesa de teste",
            Amount = 100m,
            ExpenseDate = new DateOnly(2026, 10, 1),
            Status = status,
            CreatedAtUtc = Clock.GetUtcNow().UtcDateTime,
            UpdatedAtUtc = Clock.GetUtcNow().UtcDateTime,
        };

        Repository.Expenses.Add(expense);

        return expense;
    }
}
