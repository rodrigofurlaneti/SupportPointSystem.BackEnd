using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FSI.SupportPointSystem.Infrastructure.Services;

/// <summary>
/// Implementação do serviço de hash usando BCrypt.
/// Factor de trabalho 12 (bom equilíbrio segurança/performance).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    public bool Verify(string plainPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}

/// <summary>
/// Serviço de geração de JWT com claims de Role e SellerId.
/// Validade de 8 horas conforme regra de negócio.
/// </summary>
public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    private const int TokenExpirationHours = 8;

    public string GenerateToken(User user, Seller? seller)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString().ToUpperInvariant()),
            new("cpf", user.Cpf.Value),
        };

        if (seller is not null)
        {
            claims.Add(new Claim("sellerId", seller.Id.ToString()));
            claims.Add(new Claim("sellerName", seller.Name));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(TokenExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
