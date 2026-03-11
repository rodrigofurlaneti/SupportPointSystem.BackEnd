using FSI.SupportPointSystem.Domain.Common;
using FSI.SupportPointSystem.Domain.Events;
using FSI.SupportPointSystem.Domain.Exceptions;

namespace FSI.SupportPointSystem.Domain.Entities;

/// <summary>
/// Agregado Vendedor. Raiz do agregado que engloba o perfil comercial do usuário.
/// Regras: CPF único, pode ser ativado/desativado pelo Admin.
/// </summary>
public sealed class Seller : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public bool IsActive { get; private set; }

    // Navegação
    public User? User { get; private set; }

    private Seller() { } // EF Core

    private Seller(Guid userId, string name, string? phone, string? email)
    {
        UserId = userId;
        Name = name;
        Phone = phone;
        Email = email;
        IsActive = true;
    }

    public static Seller Create(User user, string name, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome do vendedor é obrigatório.");

        var seller = new Seller(user.Id, name, phone, email);

        seller.RaiseDomainEvent(new SellerCreatedDomainEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            SellerId: seller.Id,
            SellerName: name,
            Cpf: user.Cpf.Value
        ));

        return seller;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Nome do vendedor é obrigatório.");

        Name = name;
        Phone = phone;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }
}
