using System;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Aprovação, reprovação e pagamento: role, proibição do próprio reembolso e transições de estado.
/// </summary>
[TestClass]
public sealed class ExpenseWorkflowTests
{
    private const string ValidJustification = "Recibo ilegível, reenvie por favor";

    /// <summary>
    /// O Approver aprova um reembolso enviado por outra pessoa.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_SubmittedByAnotherUser_MovesToApprovedAndRecordsHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Approver("app-1"), expense.Id, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
        Assert.HasCount(1, scenario.Repository.History);
        Assert.AreEqual(ExpenseAction.Approved, scenario.Repository.History[0].Action);
        Assert.AreEqual("app-1", scenario.Repository.History[0].ActorId);
    }

    /// <summary>
    /// O Approver não aprova o próprio reembolso, mesmo acumulando a role Employee.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_OwnExpense_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("multi-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(
            Actors.WithRoles("multi-1", "Approver", "Employee"),
            expense.Id,
            Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
        Assert.IsEmpty(scenario.Repository.History);
    }

    /// <summary>
    /// Aprovar um rascunho é uma transição inválida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_Draft_ReturnsConflict()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Draft);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Approver(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>
    /// Aprovar duas vezes retorna conflito e não duplica o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_Twice_ReturnsConflictWithoutDuplicateHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);
        await scenario.Service.ApproveAsync(Actors.Approver(), expense.Id, Scenario.Token);

        ServiceResult<ExpenseResponse> second = await scenario.Service.ApproveAsync(Actors.Approver(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, scenario.Repository.History);
    }

    /// <summary>
    /// Finance não aprova.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_AsFinance_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Finance(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Admin não aprova apenas por ser Admin.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_AsAdminOnly_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Admin(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Auditor não altera dados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_AsAuditor_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Auditor(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Reembolso inexistente retorna NotFound.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_UnknownExpense_ReturnsNotFound()
    {
        var scenario = new Scenario();

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Approver(), Guid.NewGuid(), Scenario.Token);

        Assert.AreEqual(ServiceError.NotFound, result.Error);
    }

    /// <summary>
    /// Falha de concorrência na gravação resulta em conflito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Approve_WhenSaveFails_ReturnsConflict()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);
        scenario.Repository.SaveSucceeds = false;

        ServiceResult<ExpenseResponse> result = await scenario.Service.ApproveAsync(Actors.Approver(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>
    /// A reprovação guarda a justificativa no reembolso e no histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_WithJustification_StoresReasonAndHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Approver(),
            expense.Id,
            new RejectExpenseRequest { Justification = ValidJustification },
            Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
        Assert.AreEqual(ValidJustification, expense.RejectionReason);
        Assert.AreEqual(ValidJustification, scenario.Repository.History[0].Justification);
        Assert.AreEqual(ExpenseAction.Rejected, scenario.Repository.History[0].Action);
    }

    /// <summary>
    /// Justificativa curta é rejeitada e o estado não muda.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_ShortJustification_ReturnsValidation()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Approver(),
            expense.Id,
            new RejectExpenseRequest { Justification = "curta" },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    /// <summary>
    /// Justificativa ausente é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_MissingJustification_ReturnsValidation()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Approver(),
            expense.Id,
            new RejectExpenseRequest { Justification = null },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Justificativa com mais de 500 caracteres é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_TooLongJustification_ReturnsValidation()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Approver(),
            expense.Id,
            new RejectExpenseRequest { Justification = new string('x', 501) },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// O Approver não reprova o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_OwnExpense_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("multi-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.WithRoles("multi-1", "Approver", "Employee"),
            expense.Id,
            new RejectExpenseRequest { Justification = ValidJustification },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    /// <summary>
    /// Reprovar um reembolso já aprovado é uma transição inválida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_AlreadyApproved_ReturnsConflict()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Approver(),
            expense.Id,
            new RejectExpenseRequest { Justification = ValidJustification },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
    }

    /// <summary>
    /// Reprovar duas vezes retorna conflito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_Twice_ReturnsConflictWithoutDuplicateHistory()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);
        var request = new RejectExpenseRequest { Justification = ValidJustification };
        await scenario.Service.RejectAsync(Actors.Approver(), expense.Id, request, Scenario.Token);

        ServiceResult<ExpenseResponse> second = await scenario.Service.RejectAsync(Actors.Approver(), expense.Id, request, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, scenario.Repository.History);
    }

    /// <summary>
    /// Quem não é Approver não reprova.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Reject_AsEmployee_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.RejectAsync(
            Actors.Employee("emp-2"),
            expense.Id,
            new RejectExpenseRequest { Justification = ValidJustification },
            Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// O pagamento registra o pagador, o valor e o horário do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Pay_ApprovedByFinance_RecordsPaymentWithServerData()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await scenario.Service.PayAsync(Actors.Finance("fin-1"), expense.Id, Scenario.Token);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(ExpenseStatus.Paid, expense.Status);
        Assert.HasCount(1, scenario.Repository.Payments);
        PaymentRecord payment = scenario.Repository.Payments[0];
        Assert.AreEqual("fin-1", payment.PaidByUserId);
        Assert.AreEqual(expense.Amount, payment.Amount);
        Assert.AreEqual(scenario.Clock.GetUtcNow().UtcDateTime, payment.PaidAtUtc);
        Assert.AreEqual(ExpenseAction.Paid, scenario.Repository.History[0].Action);
    }

    /// <summary>
    /// Finance não paga o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Pay_OwnExpense_ReturnsForbiddenAndCreatesNoPayment()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("multi-1", ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await scenario.Service.PayAsync(
            Actors.WithRoles("multi-1", "Finance", "Employee"),
            expense.Id,
            Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
        Assert.IsEmpty(scenario.Repository.Payments);
    }

    /// <summary>
    /// Pagar um reembolso que ainda não foi aprovado é uma transição inválida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Pay_NotApproved_ReturnsConflict()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Submitted);

        ServiceResult<ExpenseResponse> result = await scenario.Service.PayAsync(Actors.Finance(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, result.Error);
        Assert.IsEmpty(scenario.Repository.Payments);
    }

    /// <summary>
    /// Pagar duas vezes cria um único pagamento.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Pay_Twice_ReturnsConflictAndKeepsSinglePayment()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Approved);
        await scenario.Service.PayAsync(Actors.Finance(), expense.Id, Scenario.Token);

        ServiceResult<ExpenseResponse> second = await scenario.Service.PayAsync(Actors.Finance(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, second.Error);
        Assert.HasCount(1, scenario.Repository.Payments);
        Assert.HasCount(1, scenario.Repository.History);
    }

    /// <summary>
    /// Approver não paga.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Pay_AsApprover_ReturnsForbidden()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Approved);

        ServiceResult<ExpenseResponse> result = await scenario.Service.PayAsync(Actors.Approver(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Forbidden, result.Error);
    }

    /// <summary>
    /// Reembolso reprovado é um estado final: não pode ser aprovado nem pago.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task Rejected_IsFinal_CannotBeApprovedOrPaid()
    {
        var scenario = new Scenario();
        Expense expense = scenario.Seed("emp-1", ExpenseStatus.Rejected);

        ServiceResult<ExpenseResponse> approve = await scenario.Service.ApproveAsync(Actors.Approver(), expense.Id, Scenario.Token);
        ServiceResult<ExpenseResponse> pay = await scenario.Service.PayAsync(Actors.Finance(), expense.Id, Scenario.Token);

        Assert.AreEqual(ServiceError.Conflict, approve.Error);
        Assert.AreEqual(ServiceError.Conflict, pay.Error);
    }
}
