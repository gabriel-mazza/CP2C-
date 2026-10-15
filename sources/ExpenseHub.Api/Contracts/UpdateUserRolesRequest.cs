using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Conjunto final de roles do usuário. Roles ausentes da lista são removidas.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>
    /// Roles desejadas para o usuário.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public IReadOnlyList<string>? Roles { get; set; }
}
