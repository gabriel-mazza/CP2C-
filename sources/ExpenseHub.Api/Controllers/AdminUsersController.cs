using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Administração de usuários e roles (somente Admin).
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="userAdminService">Serviço de administração de usuários.</param>
    public AdminUsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    /// <summary>
    /// Lista os usuários e suas roles.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A lista de usuários.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        this.ToActionResult(await _userAdminService.ListUsersAsync(ActorContext.FromPrincipal(User), cancellationToken));

    /// <summary>
    /// Define as roles de um usuário. A pessoa precisa autenticar novamente para que valham.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="request">Roles desejadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O usuário atualizado.</returns>
    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(string id, UpdateUserRolesRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await _userAdminService.UpdateRolesAsync(ActorContext.FromPrincipal(User), id, request, cancellationToken));
}
