using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Exceptions;

namespace FSI.SupportPointSystem.Domain.ValueObjects;

/// <summary>
/// Value Object que representa um par de coordenadas geográficas (lat/lng).
/// Encapsula validação de intervalos e o cálculo de distância via Haversine.
/// </summary>
public sealed class Coordinates : ValueObject
{
    private const double EarthRadiusMeters = 6_371_000.0;

    public decimal Latitude { get; }
    public decimal Longitude { get; }

    private Coordinates() { } // EF Core

    private Coordinates(decimal latitude, decimal longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Fábrica que valida os intervalos antes de construir.</summary>
    public static Coordinates Create(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
            throw new DomainValidationException("Latitude deve estar entre -90 e 90 graus.");

        if (longitude is < -180m or > 180m)
            throw new DomainValidationException("Longitude deve estar entre -180 e 180 graus.");

        return new Coordinates(latitude, longitude);
    }

    /// <summary>
    /// Calcula a distância em metros entre este ponto e outro usando a fórmula de Haversine.
    /// </summary>
    public double DistanceInMetersTo(Coordinates other)
    {
        var dLat = ToRadians((double)(other.Latitude - Latitude));
        var dLon = ToRadians((double)(other.Longitude - Longitude));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians((double)Latitude))
              * Math.Cos(ToRadians((double)other.Latitude))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    public bool IsWithinRadiusOf(Coordinates target, double radiusMeters) =>
        DistanceInMetersTo(target) <= radiusMeters;

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    public override string ToString() => $"({Latitude}, {Longitude})";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
