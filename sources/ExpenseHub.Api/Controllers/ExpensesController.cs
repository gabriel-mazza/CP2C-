using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Operações sobre reembolsos. A role é validada aqui; ownership e estado, no serviço.
/// </summary>
[ApiController]
[Authorize]
[Route("api/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="expenseService">Serviço de reembolsos.</param>
    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    /// <summary>
    /// Cria um rascunho.
    /// </summary>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>201 com o rascunho criado.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> Create(SaveExpenseRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _expenseService.CreateAsync(ActorContext.FromPrincipal(User), request, cancellationToken);
        if (result.Value is null)
        {
            return this.ToProblem(result.Error, result.Message);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Edita um rascunho próprio.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho atualizado.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> Update(Guid id, SaveExpenseRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.UpdateAsync(ActorContext.FromPrincipal(User), id, request, cancellationToken));

    /// <summary>
    /// Lista os reembolsos visíveis ao perfil do usuário.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis.</returns>
    [HttpGet]
    [Authorize(Roles = Roles.ExpenseReaders)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.ListAsync(ActorContext.FromPrincipal(User), cancellationToken));

    /// <summary>
    /// Consulta o detalhe de um reembolso visível.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso, ou 404 quando inexistente ou fora do escopo.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.ExpenseReaders)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.GetAsync(ActorContext.FromPrincipal(User), id, cancellationToken));

    /// <summary>
    /// Envia um rascunho próprio para aprovação.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado.</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Employee)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.SubmitAsync(ActorContext.FromPrincipal(User), id, cancellationToken));

    /// <summary>
    /// Aprova um reembolso enviado.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado.</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Approver)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.ApproveAsync(ActorContext.FromPrincipal(User), id, cancellationToken));

    /// <summary>
    /// Reprova um reembolso enviado, com justificativa.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa da reprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado.</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Approver)]
    public async Task<IActionResult> Reject(Guid id, RejectExpenseRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.RejectAsync(ActorContext.FromPrincipal(User), id, request, cancellationToken));

    /// <summary>
    /// Registra o pagamento simulado de um reembolso aprovado.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso pago.</returns>
    [HttpPost("{id:guid}/pay")]
    [Authorize(Roles = Roles.Finance)]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.PayAsync(ActorContext.FromPrincipal(User), id, cancellationToken));

    /// <summary>
    /// Consulta o histórico de um reembolso visível.
    /// </summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O histórico em ordem cronológica.</returns>
    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = Roles.ExpenseReaders)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken) =>
        this.ToActionResult(await _expenseService.GetHistoryAsync(ActorContext.FromPrincipal(User), id, cancellationToken));
}
