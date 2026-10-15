using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Implementação de <see cref="IAccountService"/> sobre o ASP.NET Core Identity.
/// </summary>
internal sealed class AccountService : IAccountService
{
    private const string AuthenticationType = "Bearer";

    private readonly UserManager<IdentityUser> _userManager;

    public AccountService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        string email = request.Email.Trim();

        IdentityUser? existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return ServiceResult.Fail<UserResponse>(ServiceError.Conflict, "Já existe um usuário com este e-mail.");
        }

        var user = new IdentityUser { UserName = email, Email = email };
        IdentityResult result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            bool duplicated = result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail");
            ServiceError errorKind = duplicated ? ServiceError.Conflict : ServiceError.Validation;

            return ServiceResult.Fail<UserResponse>(errorKind, string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        return ServiceResult.Ok(new UserResponse(user.Id, user.Email, []));
    }

    public async Task<ServiceResult<ClaimsPrincipal>> AuthenticateAsync(LoginRequest request)
    {
        IdentityUser? user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return ServiceResult.Fail<ClaimsPrincipal>(ServiceError.Unauthorized, "Credenciais inválidas.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };

        IList<string> roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, AuthenticationType);

        return ServiceResult.Ok(new ClaimsPrincipal(identity));
    }
}
