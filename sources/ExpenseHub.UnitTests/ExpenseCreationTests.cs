using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Criação de rascunhos: proprietário vindo do token, validações e histórico.
/// </summary>
[TestClass]
public sealed class ExpenseCreationTests
{
    /// <summary>
    /// O rascunho nasce em Draft, pertence ao ator e usa o horário do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AsEmployee_CreatesDraftOwnedByActor()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee("emp-7"), Scenario.ValidRequest(), Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Draft", result.Value?.Status);
        Assert.AreEqual("emp-7", result.Value?.OwnerId);
        Assert.AreEqual(scenario.Clock.GetUtcNow().UtcDateTime, result.Value?.CreatedAtUtc);
        Assert.HasCount(1, scenario.Repository.Expenses);
    }

    /// <summary>
    /// A criação registra a entrada Created no histórico, com ator e horário do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_RecordsCreatedHistoryEntry()
    {
        var scenario = new Scenario();

        await scenario.Service.CreateAsync(Actors.Employee("emp-7"), Scenario.ValidRequest(), Scenario.Token);

        Assert.HasCount(1, scenario.Repository.History);
        ExpenseHistory entry = scenario.Repository.History[0];
        Assert.AreEqual(ExpenseAction.Created, entry.Action);
        Assert.AreEqual("emp-7", entry.ActorId);
        Assert.IsNull(entry.PreviousStatus);
        Assert.AreEqual(ExpenseStatus.Draft, entry.NewStatus);
        Assert.AreEqual(scenario.Clock.GetUtcNow().UtcDateTime, entry.OccurredAtUtc);
    }

    /// <summary>
    /// Admin não cria reembolso apenas por ser Admin.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AsAdminOnly_ReturnsForbidden()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Admin(), Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.IsEmpty(scenario.Repository.Expenses);
    }

    /// <summary>
    /// Approver sem a role Employee não cria reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AsApproverWithoutEmployeeRole_ReturnsForbidden()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Approver(), Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Auditor não altera dados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AsAuditor_ReturnsForbidden()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Auditor(), Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Quem acumula Approver e Employee pode criar, pois possui a role Employee.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AsApproverThatIsAlsoEmployee_Succeeds()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(
            Actors.WithRoles("multi-1", "Approver", "Employee"),
            Scenario.ValidRequest(),
            Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Descrição com menos de 10 caracteres é rejeitada e nada é gravado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_DescriptionTooShort_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Description = "curta");

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.IsEmpty(scenario.Repository.Expenses);
        Assert.IsEmpty(scenario.Repository.History);
    }

    /// <summary>
    /// Descrição com mais de 500 caracteres é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_DescriptionTooLong_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Description = new string('a', 501));

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Espaços nas pontas não contam para o tamanho mínimo da descrição.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_DescriptionOnlyWhitespacePadding_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Description = "   curta      ");

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Valor zero é rejeitado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AmountZero_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = 0m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Valor negativo é rejeitado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_NegativeAmount_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = -5m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Valor acima do máximo permitido é rejeitado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_AmountAboveMaximum_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = 2147483648m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// O valor mínimo de 0,01 é aceito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_MinimumAmount_Succeeds()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.Amount = 0.01m);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Data futura é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_FutureDate_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.ExpenseDate = new DateTime(2026, 10, 15));

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// A data de hoje é aceita, mesmo com horário posterior ao atual.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_TodayDate_Succeeds()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.ExpenseDate = new DateTime(2026, 10, 14, 23, 0, 0));

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// A data é obrigatória.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_MissingDate_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.ExpenseDate = null);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Categoria inexistente é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_UnknownCategory_ReturnsValidation()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.CategoryId = 99);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Categoria existente é aceita.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_KnownCategory_Succeeds()
    {
        var scenario = new Scenario();
        SaveExpenseRequest request = Scenario.ValidRequest(request => request.CategoryId = 1);

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), request, Scenario.Token);

        Assert.AreEqual(1, result.Value?.CategoryId);
    }

    /// <summary>
    /// Conflito de concorrência na gravação vira Conflict.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Create_WhenSaveFails_ReturnsConflict()
    {
        var scenario = new Scenario();
        scenario.Repository.SaveSucceeds = false;

        ServiceResult<ExpenseResponse> result = await scenario.Service.CreateAsync(Actors.Employee(), Scenario.ValidRequest(), Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }
}
