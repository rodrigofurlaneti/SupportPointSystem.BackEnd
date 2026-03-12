using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FSI.SupportPointSystem.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1. Localiza o caminho da API para ler o appsettings.json
        string basePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "FSI.SupportPointSystem.Api");

        // Caso você já esteja dentro da pasta src no terminal
        if (!Directory.Exists(basePath))
        {
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "FSI.SupportPointSystem.Api");
        }

        // 2. Carrega a configuração
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 3. Configura o MySQL usando a string do appsettings
        var serverVersion = ServerVersion.AutoDetect(connectionString);
        builder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(builder.Options);
    }
}