using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Cadastro e login públicos.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;

    /// <summary>
    /// Inicializa o controller.
    /// </summary>
    /// <param name="accountService">Serviço de contas.</param>
    public AuthController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Registra um usuário sem roles.
    /// </summary>
    /// <param name="request">Dados de cadastro.</param>
    /// <returns>201 com o usuário criado.</returns>
    [HttpPost("/register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        ServiceResult<UserResponse> result = await _accountService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return this.ToProblem(result.Error, result.Message);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Autentica o usuário e devolve um token bearer (accessToken).
    /// </summary>
    /// <param name="request">Credenciais.</param>
    /// <returns>O token de acesso ou 401.</returns>
    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ServiceResult<ClaimsPrincipal> result = await _accountService.AuthenticateAsync(request);
        if (result.Value is null)
        {
            return this.ToProblem(result.Error, result.Message);
        }

        return SignIn(result.Value, BearerTokenDefaults.AuthenticationScheme);
    }
}
