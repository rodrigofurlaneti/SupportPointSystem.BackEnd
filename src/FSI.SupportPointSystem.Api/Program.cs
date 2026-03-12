using FSI.SupportPointSystem.Api.Middleware;
using FSI.SupportPointSystem.Application;
using FSI.SupportPointSystem.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuração de CORS com a sua política WebAppPolicy
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebAppPolicy", policy =>
    {
        policy.AllowAnyOrigin() 
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// =========================================================
// Camadas: Application + Infrastructure
// =========================================================
builder.Services.AddHealthChecks();
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// =========================================================
// Autenticação JWT
// =========================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero // Sem margem de tolerância para expiração
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    """{"code":"UNAUTHORIZED","description":"Token JWT ausente ou inválido."}""");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    """{"code":"FORBIDDEN","description":"Acesso negado para este perfil."}""");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// =========================================================
// Swagger com suporte a JWT
// =========================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FSI.SupportPointSystem API",
        Version = "v2.0",
        Description = "Sistema de Gestão de Check-in/Check-out de Vendedores"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =========================================================
// Logging estruturado
// =========================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// --- 3. Middleware Pipeline (Ordem de Execução) ---
// 1. Tratamento de erro (deve ser o primeiro para capturar tudo)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// 2. CORS (deve vir antes de Redirection e Routing)
app.UseCors("WebAppPolicy");

// 3. Comente isso para testar localmente via HTTPapp.UseAuthentication();
app.UseHttpsRedirection(); 

// =========================================================
// Pipeline de requisições
// =========================================================
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SupportPointSystem v2.0"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }
