using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Cadastro e autenticação de usuários.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Cadastra um usuário sem nenhuma role. Roles só podem ser atribuídas por um Admin.
    /// </summary>
    /// <param name="request">Dados de cadastro.</param>
    /// <returns>O usuário criado ou o erro.</returns>
    Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Valida as credenciais e monta a identidade, com as roles atuais, usada para emitir o token.
    /// </summary>
    /// <param name="request">Credenciais.</param>
    /// <returns>A identidade autenticada ou o erro.</returns>
    Task<ServiceResult<ClaimsPrincipal>> AuthenticateAsync(LoginRequest request);
}
