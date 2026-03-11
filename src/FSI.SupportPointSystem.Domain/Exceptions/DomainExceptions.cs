namespace FSI.SupportPointSystem.Domain.Exceptions;

/// <summary>Exceção base do domínio - indica violação de invariante.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Violação de regra de validação de dados (ex: CPF inválido, coordenada fora do range).</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}

/// <summary>Violação de regra de negócio (ex: check-in fora do raio, check-in duplo).</summary>
public sealed class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message) : base(message)
    {
        RuleName = ruleName;
    }
}

/// <summary>Tentativa de acesso a recurso inexistente.</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} com Id '{id}' não encontrado.") { }

    public NotFoundException(string message) : base(message) { }
}
