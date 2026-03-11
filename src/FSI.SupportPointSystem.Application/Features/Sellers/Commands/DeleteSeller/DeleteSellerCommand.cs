using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Sellers.Commands.DeleteSeller;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record DeleteSellerResponse(
    Guid SellerId,
    string Message
);

// ============================================================
// Command
// ============================================================
/// <summary>
/// Remove (desativa) um vendedor pelo Id.
/// Regra de negócio: vendedores com visitas ativas não podem ser removidos.
/// Apenas ADMIN pode executar.
/// </summary>
public sealed record DeleteSellerCommand(Guid SellerId)
    : IRequest<Result<DeleteSellerResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class DeleteSellerCommandHandler(
    ISellerRepository sellerRepository,
    IVisitRepository visitRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSellerCommand, Result<DeleteSellerResponse>>
{
    public async Task<Result<DeleteSellerResponse>> Handle(
        DeleteSellerCommand request,
        CancellationToken cancellationToken)
    {
        var seller = await sellerRepository.GetByIdAsync(request.SellerId, cancellationToken);

        if (seller is null)
            return Result<DeleteSellerResponse>.Failure(Error.NotFound);

        var hasActiveVisit = await visitRepository.HasActiveVisitAsync(request.SellerId, cancellationToken);
        if (hasActiveVisit)
            return Result<DeleteSellerResponse>.Failure(
                Error.Custom("SELLER_HAS_ACTIVE_VISIT",
                    "Não é possível remover um vendedor com visita ativa. Encerre a visita primeiro."));

        // Desativação lógica (soft delete) para preservar histórico de visitas
        seller.Deactivate();
        sellerRepository.Update(seller);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<DeleteSellerResponse>.Success(new DeleteSellerResponse(
            SellerId: seller.Id,
            Message: "Vendedor desativado com sucesso."
        ));
    }
}
