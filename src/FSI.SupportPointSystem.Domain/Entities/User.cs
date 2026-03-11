using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.ValueObjects;

namespace FSI.SupportPointSystem.Domain.Entities;

/// <summary>
/// Entidade de credenciais de acesso ao sistema.
/// Contém apenas o necessário para autenticação e autorização.
/// </summary>
public sealed class User : Entity
{
    public Cpf Cpf { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    private User() { } // EF Core

    private User(Cpf cpf, string passwordHash, UserRole role)
    {
        Cpf = cpf;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User CreateAdmin(Cpf cpf, string passwordHash) =>
        new(cpf, passwordHash, UserRole.Admin);

    public static User CreateSeller(Cpf cpf, string passwordHash) =>
        new(cpf, passwordHash, UserRole.Seller);

    public void UpdatePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new DomainValidationException("Hash de senha não pode ser vazio.");

        PasswordHash = newHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsSeller => Role == UserRole.Seller;
}

public enum UserRole
{
    Admin = 1,
    Seller = 2
}
