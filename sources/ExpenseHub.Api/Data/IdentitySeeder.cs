using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Cria as roles obrigatórias e a única conta inicial (Admin). Pode ser executado várias vezes.
/// </summary>
internal static class IdentitySeeder
{
    public const string AdminEmailKey = "Admin:Email";

    public const string AdminPasswordKey = "Admin:Password";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        UserManager<IdentityUser> userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (string roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await EnsureSucceededAsync(roleManager.CreateAsync(new IdentityRole(roleName)), $"criar a role {roleName}");
            }
        }

        string? email = configuration[AdminEmailKey];
        string? password = configuration[AdminPasswordKey];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Configure '{AdminEmailKey}' e '{AdminPasswordKey}' (user-secrets ou variáveis de ambiente). Veja o README.");
        }

        IdentityUser? admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await EnsureSucceededAsync(userManager.CreateAsync(admin, password), "criar o Admin inicial");
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Admin))
        {
            await EnsureSucceededAsync(userManager.AddToRoleAsync(admin, Roles.Admin), "atribuir a role Admin");
        }
    }

    private static async Task EnsureSucceededAsync(Task<IdentityResult> operation, string description)
    {
        IdentityResult result = await operation;
        if (!result.Succeeded)
        {
            string errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Falha ao {description}: {errors}");
        }
    }
}
