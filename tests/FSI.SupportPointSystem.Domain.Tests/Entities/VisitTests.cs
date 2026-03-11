using FluentAssertions;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Events;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Xunit;

namespace FSI.SupportPointSystem.Domain.Tests.Entities;

public sealed class VisitTests
{
    // -------------------------------------------------------
    // Fixtures compartilhadas
    // -------------------------------------------------------
    private static Customer CreateCustomerAt(decimal lat, decimal lng)
    {
        // CNPJ válido de exemplo
        return Customer.Create("Padaria Silva", "11222333000181", lat, lng);
    }

    private static readonly Guid SellerId = Guid.NewGuid();

    // -------------------------------------------------------
    // Check-in: caminho feliz
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckin_WithinRadius_ShouldCreateOpenVisit()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var sellerLocation = Coordinates.Create(-23.550600m, -46.633400m); // ~50m

        // CORREÇÃO: parâmetro renomeado para sellerHasActiveVisit
        var visit = Visit.RegisterCheckin(SellerId, customer, sellerLocation, sellerHasActiveVisit: false);

        visit.IsOpen.Should().BeTrue();
        visit.SellerId.Should().Be(SellerId);
        visit.CustomerId.Should().Be(customer.Id);
        visit.CheckoutTimestamp.Should().BeNull();
        visit.DurationMinutes.Should().BeNull();
    }

    [Fact]
    public void RegisterCheckin_ShouldRaiseDomainEvent()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var sellerLocation = Coordinates.Create(-23.550600m, -46.633400m);

        var visit = Visit.RegisterCheckin(SellerId, customer, sellerLocation, sellerHasActiveVisit: false);

        visit.DomainEvents.Should().ContainSingle(e => e is CheckinRegisteredDomainEvent);
    }

    // -------------------------------------------------------
    // Regra: fora do raio de 100m -> 403
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckin_OutsideRadius_ShouldThrowBusinessRuleException()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var farLocation = Coordinates.Create(-23.555m, -46.638m); // ~500m

        var act = () => Visit.RegisterCheckin(SellerId, customer, farLocation, sellerHasActiveVisit: false);

        act.Should().Throw<BusinessRuleException>()
            .And.RuleName.Should().Be("OutsideCheckinRadius");
    }

    // -------------------------------------------------------
    // Regra: check-in duplo bloqueado -> 409
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckin_SellerAlreadyHasActiveVisit_ShouldThrowConflict()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var nearbyLocation = Coordinates.Create(-23.550600m, -46.633400m);

        var act = () => Visit.RegisterCheckin(SellerId, customer, nearbyLocation, sellerHasActiveVisit: true);

        act.Should().Throw<BusinessRuleException>()
            .And.RuleName.Should().Be("MultipleCheckinBlocked");
    }

    // -------------------------------------------------------
    // Check-out: caminho feliz
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckout_WithinRadius_ShouldCloseVisit()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var location = Coordinates.Create(-23.550600m, -46.633400m);
        var visit = Visit.RegisterCheckin(SellerId, customer, location, sellerHasActiveVisit: false);

        visit.RegisterCheckout(location, customer, "Visita produtiva");

        visit.IsOpen.Should().BeFalse();
        visit.CheckoutTimestamp.Should().NotBeNull();
        visit.DurationMinutes.Should().BeGreaterThanOrEqualTo(0);
        visit.CheckoutSummary.Should().Be("Visita produtiva");
    }

    [Fact]
    public void RegisterCheckout_ShouldRaiseDomainEvent()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var location = Coordinates.Create(-23.550600m, -46.633400m);
        var visit = Visit.RegisterCheckin(SellerId, customer, location, sellerHasActiveVisit: false);
        visit.ClearDomainEvents();

        visit.RegisterCheckout(location, customer, null);

        visit.DomainEvents.Should().ContainSingle(e => e is CheckoutRegisteredDomainEvent);
    }

    // -------------------------------------------------------
    // Regra: checkout fora do raio
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckout_OutsideRadius_ShouldThrow()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var checkinLocation = Coordinates.Create(-23.550600m, -46.633400m);
        var visit = Visit.RegisterCheckin(SellerId, customer, checkinLocation, sellerHasActiveVisit: false);

        var farLocation = Coordinates.Create(-23.555m, -46.638m);
        var act = () => visit.RegisterCheckout(farLocation, customer, null);

        act.Should().Throw<BusinessRuleException>()
            .And.RuleName.Should().Be("OutsideCheckinRadius");
    }

    // -------------------------------------------------------
    // Regra: checkout sem checkin (visita já fechada)
    // -------------------------------------------------------
    [Fact]
    public void RegisterCheckout_VisitAlreadyClosed_ShouldThrow()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var location = Coordinates.Create(-23.550600m, -46.633400m);
        var visit = Visit.RegisterCheckin(SellerId, customer, location, sellerHasActiveVisit: false);
        visit.RegisterCheckout(location, customer, null);

        var act = () => visit.RegisterCheckout(location, customer, null);

        act.Should().Throw<BusinessRuleException>()
            .And.RuleName.Should().Be("VisitAlreadyClosed");
    }

    // -------------------------------------------------------
    // Cálculo de duração
    // -------------------------------------------------------
    [Fact]
    public void DurationMinutes_AfterCheckout_ShouldBeNonNegative()
    {
        var customer = CreateCustomerAt(-23.550520m, -46.633308m);
        var location = Coordinates.Create(-23.550600m, -46.633400m);
        var visit = Visit.RegisterCheckin(SellerId, customer, location, sellerHasActiveVisit: false);

        visit.RegisterCheckout(location, customer, null);

        visit.DurationMinutes.Should().BeGreaterThanOrEqualTo(0);
    }
}