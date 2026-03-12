using FSI.SupportPointSystem.Application.Common.Behaviors;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Infrastructure.Persistence;
using FSI.SupportPointSystem.Infrastructure.Persistence.Repositories;
using FSI.SupportPointSystem.Infrastructure.Services;
using FSI.SupportPointSystem.Infrastructure.Services.Nominatim; // Adicione este using
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            // Verificação para evitar erro no CLI quando a string está vazia
            if (!string.IsNullOrEmpty(connectionString))
            {
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysqlOptions => mysqlOptions
                        .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                        .EnableRetryOnFailure(maxRetryCount: 3));
            }
        });

        // Repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISellerRepository, SellerRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();

        // Unit of Work e Collector
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventCollector, EfDomainEventCollector>();

        // Serviços de domínio / infraestrutura
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // --- NOVO: Registro do Nominatim com HttpClient ---
        services.AddHttpClient<IGeocodingService, NominatimService>(client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.Add("User-Agent", "FSI.SupportPointSystem.Api");
        });

        return services;
    }
}