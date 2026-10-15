using System.Collections.Generic;
using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests;

/// <summary>
/// Regras de atribuição e remoção de roles pelo Admin.
/// </summary>
[TestClass]
public sealed class RoleChangeRulesTests
{
    /// <summary>
    /// Role desconhecida é rejeitada.
    /// </summary>
    [TestMethod]
    public void Normalize_UnknownRole_ReturnsValidation()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "user-2", ["SuperUser"]);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// Nomes de role não diferenciam maiúsculas e são normalizados.
    /// </summary>
    [TestMethod]
    public void Normalize_IsCaseInsensitiveAndRemovesDuplicates()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "user-2", ["employee", "EMPLOYEE", "approver"]);

        Assert.AreEqual("Employee,Approver", string.Join(",", result.Value ?? []));
    }

    /// <summary>
    /// O Admin não remove a própria role Admin.
    /// </summary>
    [TestMethod]
    public void Normalize_RemovingOwnAdminRole_ReturnsValidation()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "admin-1", ["Employee"]);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }

    /// <summary>
    /// O Admin pode manter a própria role Admin ao alterar outras.
    /// </summary>
    [TestMethod]
    public void Normalize_KeepingOwnAdminRole_Succeeds()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "admin-1", ["Admin", "Employee"]);

        Assert.IsTrue(result.IsSuccess);
    }

    /// <summary>
    /// Remover todas as roles de outro usuário é permitido.
    /// </summary>
    [TestMethod]
    public void Normalize_ClearingRolesOfAnotherUser_Succeeds()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "user-2", []);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value ?? []);
    }

    /// <summary>
    /// Lista ausente é rejeitada.
    /// </summary>
    [TestMethod]
    public void Normalize_NullList_ReturnsValidation()
    {
        ServiceResult<IReadOnlyList<string>> result = RoleChangeRules.Normalize("admin-1", "user-2", null);

        Assert.AreEqual(ServiceError.Validation, result.Error);
    }
}
