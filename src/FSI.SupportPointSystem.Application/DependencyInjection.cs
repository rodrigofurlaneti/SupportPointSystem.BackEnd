using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FSI.SupportPointSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR com os behaviors em ordem correta:
        // Logging -> Validation -> DomainEventDispatch -> Handler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(DomainEventDispatchBehavior<,>));
        });

        // FluentValidation - registro automático de todos os validators do assembly
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
