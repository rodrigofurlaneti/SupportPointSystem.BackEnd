using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Events;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;

namespace FSI.SupportPointSystem.Domain.Entities;

/// <summary>
/// Agregado Cliente. Armazena o ponto alvo (coordenadas) para validação de proximidade.
/// Regra: CNPJ único. Coordenadas obrigatórias e válidas.
/// </summary>
public sealed class Customer : Entity
{
    public const double CheckinRadiusMeters = 100.0;

    public string CompanyName { get; private set; } = null!;
    public Cnpj Cnpj { get; private set; } = null!;
    public Coordinates LocationTarget { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Customer() { } // EF Core

    private Customer(string companyName, Cnpj cnpj, Coordinates locationTarget)
    {
        CompanyName = companyName;
        Cnpj = cnpj;
        LocationTarget = locationTarget;
        IsActive = true;
    }

    public static Customer Create(string companyName, string rawCnpj, decimal latitude, decimal longitude)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainValidationException("Razão social é obrigatória.");

        var cnpj = Cnpj.Create(rawCnpj);
        var coordinates = Coordinates.Create(latitude, longitude);

        var customer = new Customer(companyName, cnpj, coordinates);

        customer.RaiseDomainEvent(new CustomerUpsertedDomainEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            CustomerId: customer.Id,
            CompanyName: companyName,
            Cnpj: cnpj.Value
        ));

        return customer;
    }

    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        LocationTarget = Coordinates.Create(latitude, longitude);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCompanyName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainValidationException("Razão social é obrigatória.");

        CompanyName = companyName;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Verifica se as coordenadas fornecidas estão dentro do raio de check-in.</summary>
    public bool IsWithinCheckinRadius(Coordinates sellerLocation) =>
        sellerLocation.IsWithinRadiusOf(LocationTarget, CheckinRadiusMeters);
}
