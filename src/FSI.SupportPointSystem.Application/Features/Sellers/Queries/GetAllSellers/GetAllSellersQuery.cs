using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Application.Features.Sellers.Queries.GetSellerById;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Sellers.Queries.GetAllSellers;

// ============================================================
// Query
// ============================================================
/// <summary>
/// Retorna todos os vendedores ativos.
/// </summary>
public sealed record GetAllSellersQuery()
    : IRequest<Result<IReadOnlyList<SellerResponse>>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetAllSellersQueryHandler(ISellerRepository sellerRepository)
    : IRequestHandler<GetAllSellersQuery, Result<IReadOnlyList<SellerResponse>>>
{
    public async Task<Result<IReadOnlyList<SellerResponse>>> Handle(
        GetAllSellersQuery request,
        CancellationToken cancellationToken)
    {
        var sellers = await sellerRepository.GetAllActiveAsync(cancellationToken);

        var response = sellers
            .Select(s => new SellerResponse(
                SellerId: s.Id,
                UserId: s.UserId,
                Name: s.Name,
                Phone: s.Phone,
                Email: s.Email,
                IsActive: s.IsActive,
                CreatedAt: s.CreatedAt,
                UpdatedAt: s.UpdatedAt
            ))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<SellerResponse>>.Success(response);
    }
}
