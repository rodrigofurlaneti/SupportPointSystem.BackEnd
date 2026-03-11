using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Exceptions;

namespace FSI.SupportPointSystem.Domain.ValueObjects;

/// <summary>
/// Value Object para CPF com validação completa do algoritmo dos dígitos verificadores.
/// Armazena apenas os 11 dígitos numéricos.
/// </summary>
public sealed class Cpf : ValueObject
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Create(string rawValue)
    {
        var digits = ExtractDigits(rawValue);

        if (digits.Length != 11)
            throw new DomainValidationException("CPF deve conter 11 dígitos.");

        if (HasAllSameDigits(digits))
            throw new DomainValidationException("CPF inválido.");

        if (!HasValidCheckDigits(digits))
            throw new DomainValidationException("CPF inválido.");

        return new Cpf(digits);
    }

    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static string ExtractDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private static bool HasAllSameDigits(string digits) =>
        digits.Distinct().Count() == 1;

    private static bool HasValidCheckDigits(string digits)
    {
        var firstDigit = CalculateCheckDigit(digits, 9);
        var secondDigit = CalculateCheckDigit(digits, 10);
        return digits[9] - '0' == firstDigit && digits[10] - '0' == secondDigit;
    }

    private static int CalculateCheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (digits[i] - '0') * (length + 1 - i);

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
