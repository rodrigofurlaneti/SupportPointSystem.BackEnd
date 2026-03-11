using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Exceptions;

namespace FSI.SupportPointSystem.Domain.ValueObjects;

/// <summary>
/// Value Object de endereço postal para clientes e empresas.
/// </summary>
public sealed class Address : ValueObject
{
    // O sufixo = null! diz ao compilador: "Eu sei que começa nulo, mas confie em mim, será preenchido".
    public string Street { get; } = null!;
    public string Number { get; } = null!;
    public string? Complement { get; }
    public string Neighborhood { get; } = null!;
    public string City { get; } = null!;
    public string State { get; } = null!;
    public string ZipCode { get; } = null!;

    // Construtor vazio necessário para o EF Core materializar o objeto via Reflection
    private Address() { }

    private Address(
        string street, string number, string? complement,
        string neighborhood, string city, string state, string zipCode)
    {
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public static Address Create(
        string street, string number, string? complement,
        string neighborhood, string city, string state, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new DomainValidationException("Logradouro é obrigatório.");
        if (string.IsNullOrWhiteSpace(number)) throw new DomainValidationException("Número é obrigatório.");
        if (string.IsNullOrWhiteSpace(neighborhood)) throw new DomainValidationException("Bairro é obrigatório.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainValidationException("Cidade é obrigatória.");
        if (string.IsNullOrWhiteSpace(state) || state.Length != 2)
            throw new DomainValidationException("Estado deve ser a sigla de 2 letras (ex: SP).");

        var cleanZip = new string(zipCode?.Where(char.IsDigit).ToArray() ?? []);
        if (cleanZip.Length != 8) throw new DomainValidationException("CEP inválido.");

        return new Address(street, number, complement, neighborhood, city, state.ToUpperInvariant(), cleanZip);
    }

    public override string ToString() =>
        $"{Street}, {Number}{(Complement is not null ? $" {Complement}" : "")}, {Neighborhood}, {City}/{State} - {ZipCode}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return Neighborhood;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}