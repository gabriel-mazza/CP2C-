namespace ExpenseHub.Api.Domain;

/// <summary>
/// Categoria opcional de um reembolso.
/// </summary>
internal sealed class ExpenseCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
