using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Implementação de <see cref="IUserAdminService"/>.
/// </summary>
internal sealed class UserAdminService : IUserAdminService
{
    private const string ForbiddenMessage = "Somente o Admin pode administrar usuários.";

    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _context;

    public UserAdminService(UserManager<IdentityUser> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<ServiceResult<IReadOnlyList<UserResponse>>> ListUsersAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Admin))
        {
            return ServiceResult.Fail<IReadOnlyList<UserResponse>>(ServiceError.Forbidden, ForbiddenMessage);
        }

        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email })
            .ToListAsync(cancellationToken);

        var roleRows = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            select new { userRole.UserId, RoleName = role.Name })
            .ToListAsync(cancellationToken);

        ILookup<string, string> rolesByUser = roleRows.ToLookup(row => row.UserId, row => row.RoleName ?? string.Empty);

        IReadOnlyList<UserResponse> response = users
            .Select(user => new UserResponse(user.Id, user.Email, rolesByUser[user.Id].Order().ToList()))
            .ToList();

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        ActorContext actor,
        string userId,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsInRole(Roles.Admin))
        {
            return ServiceResult.Fail<UserResponse>(ServiceError.Forbidden, ForbiddenMessage);
        }

        IdentityUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail<UserResponse>(ServiceError.NotFound, "Usuário não encontrado.");
        }

        ServiceResult<IReadOnlyList<string>> desired = RoleChangeRules.Normalize(actor.UserId, user.Id, request.Roles);
        if (desired.Value is null)
        {
            return ServiceResult.Fail<UserResponse>(desired.Error, desired.Message ?? string.Empty);
        }

        IList<string> current = await _userManager.GetRolesAsync(user);
        List<string> toRemove = current.Where(role => !desired.Value.Contains(role)).ToList();
        List<string> toAdd = desired.Value.Where(role => !current.Contains(role)).ToList();

        if (toRemove.Count > 0)
        {
            IdentityResult removed = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded)
            {
                return ServiceResult.Fail<UserResponse>(ServiceError.Validation, Describe(removed));
            }
        }

        if (toAdd.Count > 0)
        {
            IdentityResult added = await _userManager.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded)
            {
                return ServiceResult.Fail<UserResponse>(ServiceError.Validation, Describe(added));
            }
        }

        IList<string> finalRoles = await _userManager.GetRolesAsync(user);

        return ServiceResult.Ok(new UserResponse(user.Id, user.Email, finalRoles.Order().ToList()));
    }

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}
