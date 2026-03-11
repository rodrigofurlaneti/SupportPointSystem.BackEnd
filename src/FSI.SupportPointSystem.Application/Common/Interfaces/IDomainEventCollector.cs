using FSI.SupportPointSystem.Domain.Common;

namespace FSI.SupportPointSystem.Application.Common.Behaviors;

/// <summary>
/// Coleta Domain Events das entidades rastreadas pelo DbContext
/// para dispatch após commit bem-sucedido.
/// </summary>
public interface IDomainEventCollector
{
    IReadOnlyList<IDomainEvent> CollectAndClear();
}
