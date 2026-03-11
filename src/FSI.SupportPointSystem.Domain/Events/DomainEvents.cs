using FSI.SupportPointSystem.Domain.Common;

namespace FSI.SupportPointSystem.Domain.Events;

/// <summary>Disparado quando um Vendedor realiza check-in com sucesso.</summary>
public sealed record CheckinRegisteredDomainEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid VisitId,
    Guid SellerId,
    Guid CustomerId,
    double DistanceMeters
) : IDomainEvent;

/// <summary>Disparado quando um Vendedor realiza check-out com sucesso.</summary>
public sealed record CheckoutRegisteredDomainEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid VisitId,
    Guid SellerId,
    Guid CustomerId,
    int DurationMinutes
) : IDomainEvent;

/// <summary>Disparado quando um novo Vendedor é cadastrado.</summary>
public sealed record SellerCreatedDomainEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid SellerId,
    string SellerName,
    string Cpf
) : IDomainEvent;

/// <summary>Disparado quando um Cliente é criado ou atualizado.</summary>
public sealed record CustomerUpsertedDomainEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid CustomerId,
    string CompanyName,
    string Cnpj
) : IDomainEvent;
