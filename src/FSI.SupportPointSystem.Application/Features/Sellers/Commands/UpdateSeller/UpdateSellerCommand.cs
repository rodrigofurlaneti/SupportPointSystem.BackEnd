using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Sellers.Commands.UpdateSeller;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record UpdateSellerResponse(
    Guid SellerId,
    string Name,
    string? Phone,
    string? Email,
    bool IsActive,
    string Message
);

// ============================================================
// Command
// ============================================================
/// <summary>
/// Atualiza o perfil de um vendedor. Suporta ativação/desativação via IsActive.
/// Apenas ADMIN pode executar.
/// </summary>
public sealed record UpdateSellerCommand(
    Guid SellerId,
    string Name,
    string? Phone,
    string? Email,
    bool IsActive
) : IRequest<Result<UpdateSellerResponse>>;

// ============================================================
// Validator
// ============================================================
public sealed class UpdateSellerCommandValidator : AbstractValidator<UpdateSellerCommand>
{
    public UpdateSellerCommandValidator()
    {
        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("Id do vendedor é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Phone)
            .Matches(@"^\d{10,11}$").WithMessage("Telefone inválido. Informe apenas dígitos (10 ou 11).")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

// ============================================================
// Handler
// ============================================================
public sealed class UpdateSellerCommandHandler(
    ISellerRepository sellerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSellerCommand, Result<UpdateSellerResponse>>
{
    public async Task<Result<UpdateSellerResponse>> Handle(
        UpdateSellerCommand request,
        CancellationToken cancellationToken)
    {
        var seller = await sellerRepository.GetByIdAsync(request.SellerId, cancellationToken);

        if (seller is null)
            return Result<UpdateSellerResponse>.Failure(Error.NotFound);

        try
        {
            seller.UpdateProfile(request.Name, request.Phone, request.Email);

            if (request.IsActive && !seller.IsActive)
                seller.Activate();
            else if (!request.IsActive && seller.IsActive)
                seller.Deactivate();
        }
        catch (Domain.Exceptions.DomainValidationException ex)
        {
            return Result<UpdateSellerResponse>.Failure(Error.Custom("DOMAIN_VALIDATION", ex.Message));
        }

        sellerRepository.Update(seller);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<UpdateSellerResponse>.Success(new UpdateSellerResponse(
            SellerId: seller.Id,
            Name: seller.Name,
            Phone: seller.Phone,
            Email: seller.Email,
            IsActive: seller.IsActive,
            Message: "Vendedor atualizado com sucesso."
        ));
    }
}
