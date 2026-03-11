using FluentAssertions;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;
using Xunit;

namespace FSI.SupportPointSystem.Domain.Tests.ValueObjects;

public sealed class CoordinatesTests
{
    // -------------------------------------------------------
    // Criação e validação
    // -------------------------------------------------------
    [Fact]
    public void Create_ValidCoordinates_ShouldSucceed()
    {
        var coords = Coordinates.Create(-23.550520m, -46.633308m);

        coords.Latitude.Should().Be(-23.550520m);
        coords.Longitude.Should().Be(-46.633308m);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    public void Create_InvalidLatitude_ShouldThrow(decimal lat, decimal lng)
    {
        var act = () => Coordinates.Create(lat, lng);
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Latitude*");
    }

    [Theory]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Create_InvalidLongitude_ShouldThrow(decimal lat, decimal lng)
    {
        var act = () => Coordinates.Create(lat, lng);
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*Longitude*");
    }

    // -------------------------------------------------------
    // Cálculo de distância - Haversine
    // -------------------------------------------------------
    [Fact]
    public void DistanceInMetersTo_SamePoint_ShouldBeZero()
    {
        var point = Coordinates.Create(-23.550520m, -46.633308m);
        point.DistanceInMetersTo(point).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void DistanceInMetersTo_KnownDistance_ShouldBeAccurate()
    {
        // Av. Paulista, SP ↔ Praça da Sé: ~2.2km
        var paulista = Coordinates.Create(-23.5614m, -46.6565m);
        var se = Coordinates.Create(-23.5503m, -46.6336m);

        var distance = paulista.DistanceInMetersTo(se);

        // Tolerância de ±100m para arredondamentos de decimal
        distance.Should().BeInRange(2000, 2500);
    }

    // -------------------------------------------------------
    // IsWithinRadiusOf - regra dos 100m
    // -------------------------------------------------------
    [Fact]
    public void IsWithinRadiusOf_Within100Meters_ShouldReturnTrue()
    {
        var target = Coordinates.Create(-23.550520m, -46.633308m);
        // ~50m de distância
        var nearby = Coordinates.Create(-23.550600m, -46.633400m);

        nearby.IsWithinRadiusOf(target, 100).Should().BeTrue();
    }

    [Fact]
    public void IsWithinRadiusOf_Beyond100Meters_ShouldReturnFalse()
    {
        var target = Coordinates.Create(-23.550520m, -46.633308m);
        // ~500m de distância
        var farAway = Coordinates.Create(-23.555m, -46.638m);

        farAway.IsWithinRadiusOf(target, 100).Should().BeFalse();
    }

    // -------------------------------------------------------
    // Igualdade de Value Objects
    // -------------------------------------------------------
    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var a = Coordinates.Create(-23.550520m, -46.633308m);
        var b = Coordinates.Create(-23.550520m, -46.633308m);

        a.Should().Be(b);
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var a = Coordinates.Create(-23.550520m, -46.633308m);
        var b = Coordinates.Create(-22.906847m, -43.172897m);

        a.Should().NotBe(b);
    }
}
