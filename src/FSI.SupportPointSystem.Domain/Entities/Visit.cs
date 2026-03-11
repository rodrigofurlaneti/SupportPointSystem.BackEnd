using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Events;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;

namespace FSI.SupportPointSystem.Domain.Entities;

/// <summary>
/// Agregado Visita. Representa o ciclo completo de check-in e check-out de um vendedor a um cliente.
/// </summary>
public sealed class Visit : Entity
{
    public Guid SellerId { get; private set; }
    public Guid CustomerId { get; private set; }

    // Check-in
    public Coordinates CheckinLocation { get; private set; } = null!; // Ajustado para null! para o compilador
    public DateTime CheckinTimestamp { get; private set; }
    public double CheckinDistanceMeters { get; private set; }

    // Check-out (nullable)
    public Coordinates? CheckoutLocation { get; private set; }
    public DateTime? CheckoutTimestamp { get; private set; }
    public double? CheckoutDistanceMeters { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? CheckoutSummary { get; private set; }

    public bool IsOpen => CheckoutTimestamp is null;

    private Visit() { } // EF Core

    private Visit(Guid sellerId, Guid customerId, Coordinates checkinLocation, double distanceMeters)
    {
        Id = Guid.NewGuid(); // Garante que a entidade tenha um ID ao ser criada
        SellerId = sellerId;
        CustomerId = customerId;
        CheckinLocation = checkinLocation;
        CheckinDistanceMeters = distanceMeters;
        CheckinTimestamp = DateTime.UtcNow;
        CreatedAt = CheckinTimestamp;
    }

    /// <summary>
    /// Registra um check-in.
    /// </summary>
    public static Visit RegisterCheckin(
        Guid sellerId,
        Customer customer,
        Coordinates sellerLocation,
        bool sellerHasActiveVisit) // Este é o nome correto do parâmetro
    {
        if (sellerHasActiveVisit)
            throw new BusinessRuleException(
                "MultipleCheckinBlocked",
                "O vendedor já possui uma visita ativa. Encerre a visita atual antes de iniciar outra.");

        var distance = sellerLocation.DistanceInMetersTo(customer.LocationTarget);

        if (!customer.IsWithinCheckinRadius(sellerLocation))
            throw new BusinessRuleException(
                "OutsideCheckinRadius",
                $"Fora do raio permitido para este cliente. Distância atual: {distance:F0}m. Raio máximo: {Customer.CheckinRadiusMeters}m.");

        var visit = new Visit(sellerId, customer.Id, sellerLocation, distance);

        visit.RaiseDomainEvent(new CheckinRegisteredDomainEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            VisitId: visit.Id,
            SellerId: sellerId,
            CustomerId: customer.Id,
            DistanceMeters: distance
        ));

        return visit;
    }

    public void RegisterCheckout(Coordinates sellerLocation, Customer customer, string? summary)
    {
        if (!IsOpen)
            throw new BusinessRuleException(
                "VisitAlreadyClosed",
                "Esta visita já foi encerrada.");

        var distance = sellerLocation.DistanceInMetersTo(customer.LocationTarget);

        if (!customer.IsWithinCheckinRadius(sellerLocation))
            throw new BusinessRuleException(
                "OutsideCheckinRadius",
                $"Fora do raio permitido para este cliente. Distância atual: {distance:F0}m. Raio máximo: {Customer.CheckinRadiusMeters}m.");

        var checkoutTime = DateTime.UtcNow;

        CheckoutLocation = sellerLocation;
        CheckoutDistanceMeters = distance;
        CheckoutTimestamp = checkoutTime;
        CheckoutSummary = summary;
        DurationMinutes = (int)(checkoutTime - CheckinTimestamp).TotalMinutes;
        UpdatedAt = checkoutTime;

        RaiseDomainEvent(new CheckoutRegisteredDomainEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: checkoutTime,
            VisitId: Id,
            SellerId: SellerId,
            CustomerId: CustomerId,
            DurationMinutes: DurationMinutes.Value
        ));
    }
}