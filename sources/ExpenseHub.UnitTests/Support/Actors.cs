using ExpenseHub.Api.Auth;

namespace ExpenseHub.UnitTests.Support;

/// <summary>
/// Atores prontos para os cenários de autorização.
/// </summary>
internal static class Actors
{
    public static ActorContext Employee(string id = "employee-1") => new(id, [Roles.Employee]);

    public static ActorContext Approver(string id = "approver-1") => new(id, [Roles.Approver]);

    public static ActorContext Finance(string id = "finance-1") => new(id, [Roles.Finance]);

    public static ActorContext Auditor(string id = "auditor-1") => new(id, [Roles.Auditor]);

    public static ActorContext Admin(string id = "admin-1") => new(id, [Roles.Admin]);

    public static ActorContext NoRole(string id = "nobody-1") => new(id, []);

    public static ActorContext WithRoles(string id, params string[] roles) => new(id, roles);
}
