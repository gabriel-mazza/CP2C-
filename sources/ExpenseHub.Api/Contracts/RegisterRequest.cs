using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Dados de cadastro. O cadastro nunca aceita roles.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// E-mail do usuário, também usado como nome de login.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Senha do usuário, validada pela política do Identity.
    /// </summary>
    [Required]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}
