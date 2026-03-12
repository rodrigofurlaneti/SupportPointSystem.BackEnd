using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Sellers.Queries.GetSellerById;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record SellerResponse(
    Guid SellerId,
    Guid UserId,
    string Name,
    string Cpf, 
    string? Phone,
    string? Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================================
// Query
// ============================================================
public sealed record GetSellerByIdQuery(Guid SellerId)
    : IRequest<Result<SellerResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetSellerByIdQueryHandler(ISellerRepository sellerRepository, IUserRepository userRepository)
    : IRequestHandler<GetSellerByIdQuery, Result<SellerResponse>>
{
    public async Task<Result<SellerResponse>> Handle(
        GetSellerByIdQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Busca o vendedor
        var seller = await sellerRepository.GetByIdAsync(request.SellerId, cancellationToken);
        if (seller is null) return Result<SellerResponse>.Failure(Error.NotFound);

        // 2. Busca o usuário vinculado para obter o CPF
        var user = await userRepository.GetByIdAsync(seller.UserId, cancellationToken);
        if (user is null) return Result<SellerResponse>.Failure(Error.NotFound);

        return Result<SellerResponse>.Success(new SellerResponse(
            SellerId: seller.Id,
            UserId: seller.UserId,
            Name: seller.Name,
            Cpf: user.Cpf.Value, // Pega o valor do Value Object CPF do Usuário
            Phone: seller.Phone,
            Email: seller.Email,
            IsActive: seller.IsActive,
            CreatedAt: seller.CreatedAt,
            UpdatedAt: seller.UpdatedAt
        ));
    }
}
