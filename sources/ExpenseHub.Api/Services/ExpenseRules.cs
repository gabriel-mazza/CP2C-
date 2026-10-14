using System;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Regras de validação de entrada dos reembolsos.
/// </summary>
internal static class ExpenseRules
{
    public const int TextMinLength = 10;

    public const int TextMaxLength = 500;

    public const decimal MinimumAmount = 0.01m;

    public const decimal MaximumAmount = int.MaxValue;

    /// <summary>
    /// Valida os dados de um rascunho.
    /// </summary>
    /// <param name="description">Descrição já sem espaços nas pontas.</param>
    /// <param name="amount">Valor informado.</param>
    /// <param name="expenseDate">Data informada.</param>
    /// <param name="today">Data atual do servidor em UTC.</param>
    /// <returns>A mensagem do primeiro erro, ou nulo quando os dados são válidos.</returns>
    public static string? ValidateExpense(string? description, decimal? amount, DateOnly? expenseDate, DateOnly today)
    {
        if (description is null || description.Length < TextMinLength || description.Length > TextMaxLength)
        {
            return $"A descrição deve ter entre {TextMinLength} e {TextMaxLength} caracteres.";
        }

        if (amount is null || amount < MinimumAmount || amount > MaximumAmount)
        {
            return "O valor deve estar entre 0,01 e 2.147.483.647,00.";
        }

        if (expenseDate is null)
        {
            return "A data da despesa é obrigatória.";
        }

        if (expenseDate > today)
        {
            return "A data da despesa não pode ser futura.";
        }

        return null;
    }

    /// <summary>
    /// Valida a justificativa de reprovação.
    /// </summary>
    /// <param name="justification">Justificativa já sem espaços nas pontas.</param>
    /// <returns>A mensagem de erro, ou nulo quando a justificativa é válida.</returns>
    public static string? ValidateJustification(string? justification)
    {
        if (justification is null || justification.Length < TextMinLength || justification.Length > TextMaxLength)
        {
            return $"A justificativa deve ter entre {TextMinLength} e {TextMaxLength} caracteres.";
        }

        return null;
    }
}
