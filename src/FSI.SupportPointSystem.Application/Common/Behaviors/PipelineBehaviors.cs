using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSI.SupportPointSystem.Application.Common.Behaviors;

/// <summary>
/// Pipeline Behavior do MediatR que intercepta todos os Commands/Queries
/// e executa a validação via FluentValidation antes de chegar ao Handler.
/// Retorna Result de falha sem chegar ao Handler se houver erros.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        logger.LogWarning("Falha de validação para {RequestType}: {@Errors}",
            typeof(TRequest).Name, failures.Select(f => f.ErrorMessage));

        // Compatível com Result<T> - lança exceção de validação para ser tratada no middleware
        throw new ValidationException(failures);
    }
}

/// <summary>
/// Pipeline Behavior para logging de performance e diagnóstico.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Iniciando {RequestName}", requestName);

        var startTime = DateTime.UtcNow;
        try
        {
            var response = await next();
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogInformation("Concluído {RequestName} em {Elapsed}ms", requestName, elapsed);
            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogError(ex, "Erro em {RequestName} após {Elapsed}ms", requestName, elapsed);
            throw;
        }
    }
}

/// <summary>
/// Pipeline Behavior para dispatch de Domain Events após commit.
/// Garante que eventos só são publicados após persistência bem-sucedida.
/// </summary>
public sealed class DomainEventDispatchBehavior<TRequest, TResponse>(
    IDomainEventCollector eventCollector,
    IPublisher publisher)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();
        var events = eventCollector.CollectAndClear();
        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, cancellationToken);

        return response;
    }
}
