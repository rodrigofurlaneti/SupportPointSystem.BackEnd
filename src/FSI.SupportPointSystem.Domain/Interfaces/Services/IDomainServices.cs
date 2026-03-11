using FSI.SupportPointSystem.Domain.Entities;

namespace FSI.SupportPointSystem.Domain.Interfaces.Services;

/// <summary>
/// Unit of Work: garante consistência transacional entre repositórios.
/// </summary>
public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Serviço de criptografia de senhas (one-way hash).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}

/// <summary>
/// Serviço de geração e validação de tokens JWT.
/// </summary>
public interface ITokenService
{
    string GenerateToken(User user, Seller? seller);
}
