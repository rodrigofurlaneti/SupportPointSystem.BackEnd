using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FSI.SupportPointSystem.Infrastructure.Persistence;

/// <summary>
/// DbContext principal. Aplica configurações via IEntityTypeConfiguration.
/// Responsável pelo dispatch de Domain Events após SaveChanges.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<Seller> Sellers { get; init; }
    public DbSet<Customer> Customers { get; init; }
    public DbSet<Visit> Visits { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as configurações do assembly automaticamente (IEntityTypeConfiguration<T>)
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Salva mudanças e retorna os Domain Events coletados das entidades.
    /// O dispatch é feito pelo DomainEventDispatchBehavior após o commit.
    /// </summary>
    public IReadOnlyList<IDomainEvent> CollectDomainEvents()
    {
        var entities = ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());
        return events;
    }
}
