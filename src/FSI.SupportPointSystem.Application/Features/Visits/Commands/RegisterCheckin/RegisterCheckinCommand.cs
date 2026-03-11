using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckin;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record CheckinResponse(
    Guid VisitId,
    Guid SellerId,
    Guid CustomerId,
    double DistanceMeters,
    DateTime CheckinTimestamp,
    string Message
);

// ============================================================
// Command (CQRS - escrita)
// ============================================================
/// <summary>
/// Command para registrar um check-in de vendedor.
/// Carrega as coordenadas atuais do vendedor e o Id do cliente alvo.
/// </summary>
public sealed record RegisterCheckinCommand(
    Guid SellerId,
    Guid CustomerId,
    decimal Latitude,
    decimal Longitude
) : IRequest<Result<CheckinResponse>>;

// ============================================================
// Validator (FluentValidation)
// ============================================================
public sealed class RegisterCheckinCommandValidator : AbstractValidator<RegisterCheckinCommand>
{
    public RegisterCheckinCommandValidator()
    {
        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId é obrigatório.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId é obrigatório.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude deve estar entre -90 e 90 graus.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude deve estar entre -180 e 180 graus.");
    }
}

// ============================================================
// Handler
// ============================================================
public sealed class RegisterCheckinCommandHandler(
    IVisitRepository visitRepository,
    ICustomerRepository customerRepository,
    ISellerRepository sellerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCheckinCommand, Result<CheckinResponse>>
{
    public async Task<Result<CheckinResponse>> Handle(
        RegisterCheckinCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Vendedor deve existir e estar ativo
        var seller = await sellerRepository.GetByIdAsync(request.SellerId, cancellationToken);
        if (seller is null)
            return Result<CheckinResponse>.Failure(Error.Custom("SELLER_NOT_FOUND", "Vendedor não encontrado."));

        if (!seller.IsActive)
            return Result<CheckinResponse>.Failure(Error.Custom("SELLER_INACTIVE", "Vendedor está inativo."));

        // 2. Cliente deve existir
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Result<CheckinResponse>.Failure(Error.Custom("CUSTOMER_NOT_FOUND", "Cliente não encontrado."));

        // 3. Verificar se vendedor já tem visita ativa (regra de negócio)
        var hasActiveVisit = await visitRepository.HasActiveVisitAsync(request.SellerId, cancellationToken);

        // 4. Criar coordenadas do vendedor
        Coordinates sellerLocation;
        try
        {
            sellerLocation = Coordinates.Create(request.Latitude, request.Longitude);
        }
        catch (DomainValidationException ex)
        {
            return Result<CheckinResponse>.Failure(Error.Custom("INVALID_COORDINATES", ex.Message));
        }

        // 5. Delegar a regra de negócio ao Agregado Visit
        Visit visit;
        try
        {
            visit = Visit.RegisterCheckin(request.SellerId, customer, sellerLocation, hasActiveVisit);
        }
        catch (BusinessRuleException ex)
        {
            var error = ex.RuleName switch
            {
                "MultipleCheckinBlocked" => Error.Custom("CONFLICT_CHECKIN", ex.Message),
                "OutsideCheckinRadius"   => Error.Custom("OUTSIDE_RADIUS", ex.Message),
                _                        => Error.Custom(ex.RuleName, ex.Message)
            };
            return Result<CheckinResponse>.Failure(error);
        }

        // 6. Persistir e commitar
        await visitRepository.AddAsync(visit, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<CheckinResponse>.Success(new CheckinResponse(
            VisitId: visit.Id,
            SellerId: visit.SellerId,
            CustomerId: visit.CustomerId,
            DistanceMeters: visit.CheckinDistanceMeters,
            CheckinTimestamp: visit.CheckinTimestamp,
            Message: "Check-in realizado com sucesso."
        ));
    }
}
