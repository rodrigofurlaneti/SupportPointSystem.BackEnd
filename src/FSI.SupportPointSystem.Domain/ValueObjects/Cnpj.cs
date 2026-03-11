using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Exceptions;

namespace FSI.SupportPointSystem.Domain.ValueObjects;

/// <summary>
/// Value Object para CNPJ com validação dos dígitos verificadores.
/// </summary>
public sealed class Cnpj : ValueObject
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string rawValue)
    {
        var digits = new string(rawValue.Where(char.IsDigit).ToArray());

        if (digits.Length != 14)
            throw new DomainValidationException("CNPJ deve conter 14 dígitos.");

        if (digits.Distinct().Count() == 1)
            throw new DomainValidationException("CNPJ inválido.");

        if (!HasValidCheckDigits(digits))
            throw new DomainValidationException("CNPJ inválido.");

        return new Cnpj(digits);
    }

    public string Formatted =>
        $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool HasValidCheckDigits(string digits)
    {
        int[] multipliers1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] multipliers2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        var firstDigit = CalcDigit(digits, multipliers1);
        var secondDigit = CalcDigit(digits, multipliers2);

        return digits[12] - '0' == firstDigit && digits[13] - '0' == secondDigit;
    }

    private static int CalcDigit(string digits, int[] multipliers)
    {
        var sum = multipliers.Select((m, i) => (digits[i] - '0') * m).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
