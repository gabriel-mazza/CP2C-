using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Dados de criação ou edição de um rascunho. Proprietário, estado, ator e horários
/// são sempre definidos pelo servidor.
/// </summary>
public sealed class SaveExpenseRequest
{
    /// <summary>
    /// Descrição de 10 a 500 caracteres.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string? Description { get; set; }

    /// <summary>
    /// Valor entre 0,01 e 2.147.483.647,00.
    /// </summary>
    [Required]
    [Range(typeof(decimal), "0.01", "2147483647", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Data da despesa (não pode ser futura).
    /// </summary>
    [Required]
    public DateTime? ExpenseDate { get; set; }

    /// <summary>
    /// Categoria opcional.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }
}
