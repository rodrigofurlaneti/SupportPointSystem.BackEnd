using FSI.SupportPointSystem.Application.Common.Behaviors;
using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Interfaces.Services;

namespace FSI.SupportPointSystem.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementado sobre o AppDbContext.
/// Um único SaveChangesAsync garante consistência transacional.
/// </summary>
public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}

/// <summary>
/// Coleta Domain Events das entidades rastreadas pelo EF Core.
/// Injetado no DomainEventDispatchBehavior para dispatch pós-commit.
/// </summary>
public sealed class EfDomainEventCollector(AppDbContext context) : IDomainEventCollector
{
    public IReadOnlyList<IDomainEvent> CollectAndClear() =>
        context.CollectDomainEvents();
}
