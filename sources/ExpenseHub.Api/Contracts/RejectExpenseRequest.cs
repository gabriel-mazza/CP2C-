using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Justificativa obrigatória da reprovação.
/// </summary>
public sealed class RejectExpenseRequest
{
    /// <summary>
    /// Justificativa de 10 a 500 caracteres.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string? Justification { get; set; }
}
