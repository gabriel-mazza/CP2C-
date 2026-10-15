using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Administração de usuários e roles, restrita ao Admin.
/// </summary>
public interface IUserAdminService
{
    /// <summary>
    /// Lista os usuários e suas roles.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os usuários ou o erro.</returns>
    Task<ServiceResult<IReadOnlyList<UserResponse>>> ListUsersAsync(ActorContext actor, CancellationToken cancellationToken);

    /// <summary>
    /// Define o conjunto de roles de um usuário, adicionando e removendo o necessário.
    /// </summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="userId">Usuário que terá as roles alteradas.</param>
    /// <param name="request">Roles desejadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuário atualizado ou o erro.</returns>
    Task<ServiceResult<UserResponse>> UpdateRolesAsync(ActorContext actor, string userId, UpdateUserRolesRequest request, CancellationToken cancellationToken);
}
