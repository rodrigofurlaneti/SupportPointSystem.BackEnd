namespace FSI.SupportPointSystem.Domain.Common;

/// <summary>
/// Classe base para Value Objects.
/// Igualdade estrutural baseada nos componentes (sem identidade própria).
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Retorna os componentes que definem a igualdade do objeto.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(17, (current, obj) =>
            {
                unchecked
                {
                    return current * 31 + (obj?.GetHashCode() ?? 0);
                }
            });
    }
}