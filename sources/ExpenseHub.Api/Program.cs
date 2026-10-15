using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    private const string DefaultConnectionString = "Data Source=expensehub.db";

    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString = builder.Configuration.GetConnectionString("Default") ?? DefaultConnectionString;
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        builder.Services
            .AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services
            .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddBearerToken(BearerTokenDefaults.AuthenticationScheme);
        builder.Services.AddAuthorization();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
        builder.Services.AddScoped<IExpenseService, ExpenseService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IUserAdminService, UserAdminService>();

        WebApplication app = builder.Build();

        await InitializeDatabaseAsync(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        await app.RunAsync();
    }

    private static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        await IdentitySeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    }
}
