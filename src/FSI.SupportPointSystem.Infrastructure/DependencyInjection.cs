using FSI.SupportPointSystem.Application.Common.Behaviors;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Infrastructure.Persistence;
using FSI.SupportPointSystem.Infrastructure.Persistence.Repositories;
using FSI.SupportPointSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FSI.SupportPointSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core - SQL Server
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                          .EnableRetryOnFailure(maxRetryCount: 3)));

        // Repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISellerRepository, SellerRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain Event Collector (para o behavior do MediatR)
        services.AddScoped<IDomainEventCollector, EfDomainEventCollector>();

        // Serviços de domínio / infraestrutura
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
