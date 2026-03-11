using MediatR;

namespace FSI.SupportPointSystem.Domain.Common;

/// <summary>
/// Marcador para Domain Events. Implementa INotification do MediatR
/// permitindo que handlers reativos sejam registrados sem acoplamento.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
