namespace FSI.SupportPointSystem.Application.Common.Results;

/// <summary>
/// Result Pattern: encapsula sucesso ou falha sem lançar exceções para controle de fluxo.
/// Elimina o uso de try/catch na camada de Application para erros esperados.
/// </summary>
public sealed class Result<T>
{
    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = Error.None;
    }

    private Result(Error error)
    {
        Value = default;
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    // Erros de domínio padronizados
    public static readonly Error NotFound = new("NOT_FOUND", "Recurso não encontrado.");
    public static readonly Error Unauthorized = new("UNAUTHORIZED", "Credenciais inválidas.");
    public static readonly Error Forbidden = new("FORBIDDEN", "Acesso negado.");
    public static readonly Error Conflict = new("CONFLICT", "Conflito de estado.");
    public static readonly Error Validation = new("VALIDATION", "Dados inválidos.");

    public static Error Custom(string code, string description) => new(code, description);
}
