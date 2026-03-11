using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSI.SupportPointSystem.Application.Common.Behaviors;

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

        throw new ValidationException(failures);
    }
}

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
        logger.LogInformation(" [MÉTODO INICIADO] {RequestName}", requestName);

        var startTime = DateTime.UtcNow;
        try
        {
            var response = await next();
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogInformation(" [MÉTODO SUCESSO] {RequestName} em {Elapsed}ms", requestName, elapsed);
            return response;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.LogCritical(" [ERRO CRÍTICO] Falha na execução de {RequestName} após {Elapsed}ms", requestName, elapsed);
            logger.LogError("MENSAGEM: {Message}", ex.Message);
            throw;
        }
    }
}

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