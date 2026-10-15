using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Credenciais de login.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// E-mail cadastrado.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário.
    /// </summary>
    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
