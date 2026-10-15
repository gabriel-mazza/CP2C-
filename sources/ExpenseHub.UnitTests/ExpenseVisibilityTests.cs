using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Escopo de leitura por perfil, detalhe (404 fora do escopo) e histórico.
/// </summary>
[TestClass]
public sealed class ExpenseVisibilityTests
{
    private static Scenario SeedAllStatuses(out Expense draft, out Expense submitted, out Expense approved)
    {
        var scenario = new Scenario();
        draft = scenario.Seed("emp-1", ExpenseStatus.Draft);
        submitted = scenario.Seed("emp-1", ExpenseStatus.Submitted);
        approved = scenario.Seed("emp-1", ExpenseStatus.Approved);
        scenario.Seed("emp-1", ExpenseStatus.Paid);
        scenario.Seed("emp-1", ExpenseStatus.Rejected);
        scenario.Seed("emp-2", ExpenseStatus.Draft);

        return scenario;
    }

    private static async Task<IReadOnlyList<ExpenseResponse>> ListAsync(Scenario scenario, Api.Auth.ActorContext actor)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await scenario.Service.ListAsync(actor, Scenario.Token);

        return result.Value ?? [];
    }

    /// <summary>
    /// Employee vê somente os próprios reembolsos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsEmployee_ReturnsOnlyOwnExpenses()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        IReadOnlyList<ExpenseResponse> items = await ListAsync(scenario, Actors.Employee("emp-1"));

        Assert.HasCount(5, items);
        Assert.IsTrue(items.All(item => item.OwnerId == "emp-1"));
    }

    /// <summary>
    /// Approver vê somente reembolsos enviados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsApprover_ReturnsOnlySubmitted()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        IReadOnlyList<ExpenseResponse> items = await ListAsync(scenario, Actors.Approver());

        Assert.HasCount(1, items);
        Assert.AreEqual("Submitted", items[0].Status);
    }

    /// <summary>
    /// Finance vê somente reembolsos aprovados e pagos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsFinance_ReturnsOnlyApprovedAndPaid()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        IReadOnlyList<ExpenseResponse> items = await ListAsync(scenario, Actors.Finance());

        Assert.HasCount(2, items);
        Assert.IsTrue(items.All(item => item.Status is "Approved" or "Paid"));
    }

    /// <summary>
    /// Auditor vê todos os reembolsos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsAuditor_ReturnsEverything()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        IReadOnlyList<ExpenseResponse> items = await ListAsync(scenario, Actors.Auditor());

        Assert.HasCount(6, items);
    }

    /// <summary>
    /// As roles acumuladas somam permissões: Approver e Employee veem enviados e os próprios.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsApproverAndEmployee_ReturnsUnionOfScopes()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        IReadOnlyList<ExpenseResponse> items = await ListAsync(scenario, Actors.WithRoles("emp-2", "Approver", "Employee"));

        Assert.HasCount(2, items);
    }

    /// <summary>
    /// Admin, por si só, não lista reembolsos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_AsAdminOnly_ReturnsForbidden()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await scenario.Service.ListAsync(Actors.Admin(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Usuário autenticado sem role não lista reembolsos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task List_WithoutAnyRole_ReturnsForbidden()
    {
        Scenario scenario = SeedAllStatuses(out _, out _, out _);

        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await scenario.Service.ListAsync(Actors.NoRole(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// O proprietário consulta o próprio detalhe.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Get_OwnExpense_ReturnsDetail()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<ExpenseResponse> result = await scenario.Service.GetAsync(Actors.Employee("emp-1"), draft.Id, Scenario.Token);

        Assert.AreEqual(draft.Id, result.Value?.Id);
    }

    /// <summary>
    /// Outro Employee recebe 404, sem saber que o reembolso existe.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Get_OtherEmployeesExpense_ReturnsNotFound()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<ExpenseResponse> result = await scenario.Service.GetAsync(Actors.Employee("emp-2"), draft.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>
    /// Approver não vê rascunhos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Get_AsApproverDraft_ReturnsNotFound()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<ExpenseResponse> result = await scenario.Service.GetAsync(Actors.Approver(), draft.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>
    /// Finance não vê reembolsos apenas enviados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Get_AsFinanceSubmitted_ReturnsNotFound()
    {
        Scenario scenario = SeedAllStatuses(out _, out Expense submitted, out _);

        ServiceResult<ExpenseResponse> result = await scenario.Service.GetAsync(Actors.Finance(), submitted.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>
    /// Auditor consulta qualquer reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Get_AsAuditor_ReturnsAnyExpense()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<ExpenseResponse> result = await scenario.Service.GetAsync(Actors.Auditor(), draft.Id, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// O histórico segue a ordem cronológica do fluxo completo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task History_FullFlow_IsChronologicalAndVisibleToOwner()
    {
        var scenario = new Scenario();
        ServiceResult<ExpenseResponse> created = await scenario.Service.CreateAsync(Actors.Employee("emp-1"), Scenario.ValidRequest(), Scenario.Token);
        Guid id = created.Value?.Id ?? Guid.Empty;
        scenario.Clock.Advance(TimeSpan.FromMinutes(1));
        await scenario.Service.SubmitAsync(Actors.Employee("emp-1"), id, Scenario.Token);
        scenario.Clock.Advance(TimeSpan.FromMinutes(1));
        await scenario.Service.ApproveAsync(Actors.Approver(), id, Scenario.Token);
        scenario.Clock.Advance(TimeSpan.FromMinutes(1));
        await scenario.Service.PayAsync(Actors.Finance(), id, Scenario.Token);

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> history = await scenario.Service.GetHistoryAsync(Actors.Employee("emp-1"), id, Scenario.Token);

        string actions = string.Join(",", (history.Value ?? []).Select(entry => entry.Action));
        Assert.AreEqual("Created,Submitted,Approved,Paid", actions);
    }

    /// <summary>
    /// O histórico segue a mesma visibilidade do reembolso: outro Employee recebe 404.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task History_OtherEmployee_ReturnsNotFound()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await scenario.Service.GetHistoryAsync(Actors.Employee("emp-2"), draft.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>
    /// Admin, por si só, não consulta histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task History_AsAdminOnly_ReturnsForbidden()
    {
        Scenario scenario = SeedAllStatuses(out Expense draft, out _, out _);

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await scenario.Service.GetHistoryAsync(Actors.Admin(), draft.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }
}
