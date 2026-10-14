using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core: tabelas do Identity e do domínio de reembolsos.
/// </summary>
internal sealed class AppDbContext : IdentityDbContext<IdentityUser>
{
    private const int KeyLength = 450;
    private const int StatusLength = 20;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<ExpenseCategory> Categories => Set<ExpenseCategory>();

    public DbSet<ExpenseHistory> ExpenseHistories => Set<ExpenseHistory>();

    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(category => category.Name).IsUnique();
            entity.HasData(
                new ExpenseCategory { Id = 1, Name = "Transporte" },
                new ExpenseCategory { Id = 2, Name = "Alimentação" },
                new ExpenseCategory { Id = 3, Name = "Hospedagem" },
                new ExpenseCategory { Id = 4, Name = "Outros" });
        });

        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(expense => expense.Id);
            entity.Property(expense => expense.OwnerId).IsRequired().HasMaxLength(KeyLength);
            entity.Property(expense => expense.Description).IsRequired().HasMaxLength(ExpenseRules.TextMaxLength);
            entity.Property(expense => expense.Amount).HasPrecision(18, 2);
            entity.Property(expense => expense.RejectionReason).HasMaxLength(ExpenseRules.TextMaxLength);

            // O estado também é token de concorrência: duas aprovações simultâneas não passam juntas.
            entity.Property(expense => expense.Status)
                .HasConversion<string>()
                .HasMaxLength(StatusLength)
                .IsConcurrencyToken();

            entity.HasIndex(expense => expense.OwnerId);
            entity.HasIndex(expense => expense.Status);

            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(expense => expense.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ExpenseCategory>()
                .WithMany()
                .HasForeignKey(expense => expense.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ExpenseHistory>(entity =>
        {
            entity.HasKey(history => history.Id);
            entity.Property(history => history.Id).ValueGeneratedOnAdd();
            entity.Property(history => history.Action).HasConversion<string>().HasMaxLength(StatusLength);
            entity.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(StatusLength);
            entity.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(StatusLength);
            entity.Property(history => history.ActorId).IsRequired().HasMaxLength(KeyLength);
            entity.Property(history => history.Justification).HasMaxLength(ExpenseRules.TextMaxLength);
            entity.Property(history => history.Details).HasMaxLength(2000);
            entity.HasIndex(history => history.ExpenseId);

            entity.HasOne<Expense>()
                .WithMany()
                .HasForeignKey(history => history.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.PaidByUserId).IsRequired().HasMaxLength(KeyLength);
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);
            entity.HasIndex(payment => payment.ExpenseId).IsUnique();

            entity.HasOne<Expense>()
                .WithMany()
                .HasForeignKey(payment => payment.ExpenseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
