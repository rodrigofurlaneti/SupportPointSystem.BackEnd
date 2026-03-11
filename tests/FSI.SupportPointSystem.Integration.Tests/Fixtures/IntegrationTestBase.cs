using FluentAssertions;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using FSI.SupportPointSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FSI.SupportPointSystem.Integration.Tests.Fixtures;

/// <summary>
/// Factory que substitui o banco SQL Server por banco em memória.
/// Usado em todos os testes de integração.
/// </summary>
public sealed class SupportPointSystemWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover o DbContext registrado (SQL Server)
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            // Substituir por InMemory
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>Cria e migra um banco de teste limpo, semeando dados iniciais.</summary>
    public async Task SeedDatabaseAsync(Action<AppDbContext, IPasswordHasher> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await db.Database.EnsureCreatedAsync();
        seed(db, hasher);
        await db.SaveChangesAsync();
    }
}

// ============================================================
// Testes de integração - Auth
// ============================================================
public sealed class AuthControllerIntegrationTests : IClassFixture<SupportPointSystemWebFactory>
{
    private readonly SupportPointSystemWebFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(SupportPointSystemWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndToken()
    {
        await _factory.SeedDatabaseAsync((db, hasher) =>
        {
            var user = User.CreateAdmin(Cpf.Create("52998224725"), hasher.Hash("senha123!"));
            db.Users.Add(user);
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            cpf = "52998224725",
            password = "senha123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("userRole").GetString().Should().Be("ADMIN");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        await _factory.SeedDatabaseAsync((db, hasher) =>
        {
            var user = User.CreateAdmin(Cpf.Create("52998224725"), hasher.Hash("correta123!"));
            db.Users.Add(user);
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            cpf = "52998224725",
            password = "errada999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentCpf_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            cpf = "00000000000",
            password = "qualquer"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCustomer_WithoutToken_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new
        {
            companyName = "Empresa X",
            cnpj = "11222333000181",
            latitude = -23.5m,
            longitude = -46.6m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostSeller_AsSellerRole_ShouldReturn403()
    {
        // Vendedor não pode criar outros vendedores
        var sellerId = await CreateAndLoginSellerAsync();

        var response = await _client.PostAsJsonAsync("/api/sellers", new
        {
            cpf = "52998224725",
            password = "senha123!",
            name = "Novo Vendedor"
        });

        // Seller tentando acessar rota de ADMIN
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private async Task<string> CreateAndLoginSellerAsync()
    {
        var sellerCpf = "22233344455";
        await _factory.SeedDatabaseAsync((db, hasher) =>
        {
            var user = User.CreateSeller(Cpf.Create(sellerCpf), hasher.Hash("senha123!"));
            var seller = Seller.Create(user, "Vendedor Teste", null, null);
            db.Users.Add(user);
            db.Sellers.Add(seller);
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            cpf = sellerCpf,
            password = "senha123!"
        });

        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return token;
    }
}
