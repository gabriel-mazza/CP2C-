using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Edição e envio: ownership, estado Draft e histórico.
/// </summary>
[TestClass]
public sealed class ExpenseEditAndSubmitTests
{
    /// <summary>
    /// O proprietário edita o próprio rascunho e a alteração entra no histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_OwnerDraft_UpdatesFieldsAndRecordsHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = 75m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Employee("emp-1"), expense.Id, request, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(75m, expense.Amount);
        Assert.HasCount(1, scenario.Repository.History);
        Assert.AreEqual(ExpenseAction.Edited, scenario.Repository.History[0].Action);
        Assert.IsNotNull(scenario.Repository.History[0].Details);
    }

    /// <summary>
    /// Edição sem nenhuma mudança não gera histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_WithoutChanges_DoesNotAddHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);
        var request = new SaveExpenseRequest
        {
            Description = "Despesa de teste",
            Amount = 100m,
            ExpenseDate = new System.DateTime(2026, 10, 1),
        };

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Employee("emp-1"), expense.Id, request, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(scenario.Repository.History);
    }

    /// <summary>
    /// Outro Employee não acessa o reembolso (404, sem revelar que existe).
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_OtherEmployee_ReturnsNotFoundAndKeepsData()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Employee("emp-2"), expense.Id, Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
        Assert.AreEqual(100m, expense.Amount);
    }

    /// <summary>
    /// Reembolso já enviado não pode mais ser editado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_SubmittedExpense_ReturnsConflict()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Employee("emp-1"), expense.Id, Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>
    /// Approver que também é Employee enxerga o reembolso enviado de outro, mas não pode editá-lo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_VisibleButNotOwned_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(
            Actors.WithRoles("multi-1", "Approver", "Employee"),
            expense.Id,
            Scenario.ValidRequest(),
            Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Sem a role Employee não há edição.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_WithoutEmployeeRole_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Auditor(), expense.Id, Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Entrada inválida mantém os dados originais.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Update_InvalidInput_ReturnsValidationAndKeepsData()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = -1m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.UpdateAsync(Actors.Employee("emp-1"), expense.Id, request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.AreEqual(100m, expense.Amount);
    }

    /// <summary>
    /// O proprietário envia o rascunho e a transição fica no histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Submit_OwnerDraft_MovesToSubmittedAndRecordsHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.SubmitAsync(Actors.Employee("emp-1"), expense.Id, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.HasCount(1, scenario.Repository.History);
        Assert.AreEqual(ExpenseStatus.Draft, scenario.Repository.History[0].PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, scenario.Repository.History[0].NewStatus);
    }

    /// <summary>
    /// Enviar duas vezes retorna conflito e não duplica o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Submit_Twice_ReturnsConflictWithoutDuplicateHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);
        await scenario.Service.SubmitAsync(Actors.Employee("emp-1"), expense.Id, Scenario.Token);

        ServiceResult<ExpenseResponse> second = await scenario.Service.SubmitAsync(Actors.Employee("emp-1"), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, scenario.Repository.History);
    }

    /// <summary>
    /// Outro Employee não envia o rascunho alheio.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Submit_OtherEmployee_ReturnsNotFound()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.SubmitAsync(Actors.Employee("emp-2"), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
    }

    /// <summary>
    /// Sem a role Employee não há envio.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Submit_WithoutEmployeeRole_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.SubmitAsync(Actors.Admin(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Reembolso inexistente retorna NotFound.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Submit_UnknownExpense_ReturnsNotFound()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.SubmitAsync(Actors.Employee(), System.Guid.NewGuid(), Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }
}
