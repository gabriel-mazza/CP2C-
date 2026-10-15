using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Casos de uso dos reembolsos. Aplica, na ordem: role, escopo de leitura, ownership,
/// estado atual e validação de entrada.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Cria um rascunho pertencente ao usuário autenticado.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho criado ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> CreateAsync(ActorContext actor, SaveExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Edita um rascunho próprio.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O rascunho atualizado ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> UpdateAsync(ActorContext actor, Guid id, SaveExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lista os reembolsos visíveis ao usuário.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis ou o erro.</returns>
    Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(ActorContext actor, CancellationToken cancellationToken);

    /// <summary>
    /// Consulta o detalhe de um reembolso visível ao usuário.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> GetAsync(ActorContext actor, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Envia um rascunho próprio para aprovação.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> SubmitAsync(ActorContext actor, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Aprova um reembolso enviado que não pertença ao aprovador.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> ApproveAsync(ActorContext actor, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Reprova, com justificativa, um reembolso enviado que não pertença ao aprovador.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa da reprovação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> RejectAsync(ActorContext actor, Guid id, RejectExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Registra o pagamento simulado de um reembolso aprovado que não pertença ao financeiro.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso pago ou o erro.</returns>
    Task<ServiceResult<ExpenseResponse>> PayAsync(ActorContext actor, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Consulta o histórico de um reembolso visível ao usuário.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O histórico em ordem cronológica ou o erro.</returns>
    Task<ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>> GetHistoryAsync(ActorContext actor, Guid id, CancellationToken cancellationToken);
}
