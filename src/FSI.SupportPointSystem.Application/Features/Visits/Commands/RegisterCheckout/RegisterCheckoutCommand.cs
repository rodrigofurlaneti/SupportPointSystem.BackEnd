using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Exceptions;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckout;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record CheckoutResponse(
    Guid VisitId,
    Guid SellerId,
    double DistanceMeters,
    DateTime CheckinTimestamp,
    DateTime CheckoutTimestamp,
    int DurationMinutes,
    string Message
);

// ============================================================
// Command
// ============================================================
public sealed record RegisterCheckoutCommand(
    Guid SellerId,
    decimal Latitude,
    decimal Longitude,
    string? Summary
) : IRequest<Result<CheckoutResponse>>;

// ============================================================
// Validator
// ============================================================
public sealed class RegisterCheckoutCommandValidator : AbstractValidator<RegisterCheckoutCommand>
{
    public RegisterCheckoutCommandValidator()
    {
        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId é obrigatório.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude deve estar entre -90 e 90 graus.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude deve estar entre -180 e 180 graus.");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Resumo não pode ultrapassar 500 caracteres.")
            .When(x => x.Summary is not null);
    }
}

// ============================================================
// Handler
// ============================================================
public sealed class RegisterCheckoutCommandHandler(
    IVisitRepository visitRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCheckoutCommand, Result<CheckoutResponse>>
{
    public async Task<Result<CheckoutResponse>> Handle(
        RegisterCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar visita ativa do vendedor
        var visit = await visitRepository.GetActiveVisitBySellerIdAsync(request.SellerId, cancellationToken);
        if (visit is null)
            return Result<CheckoutResponse>.Failure(
                Error.Custom("NO_ACTIVE_VISIT", "Não existe uma visita ativa para este vendedor."));

        // 2. Buscar cliente da visita
        var customer = await customerRepository.GetByIdAsync(visit.CustomerId, cancellationToken);
        if (customer is null)
            return Result<CheckoutResponse>.Failure(
                Error.Custom("CUSTOMER_NOT_FOUND", "Dados do cliente da visita não encontrados."));

        // 3. Montar coordenadas atuais do vendedor
        Coordinates sellerLocation;
        try
        {
            sellerLocation = Coordinates.Create(request.Latitude, request.Longitude);
        }
        catch (DomainValidationException ex)
        {
            return Result<CheckoutResponse>.Failure(Error.Custom("INVALID_COORDINATES", ex.Message));
        }

        // 4. Delegar ao Agregado a regra de checkout
        try
        {
            visit.RegisterCheckout(sellerLocation, customer, request.Summary);
        }
        catch (BusinessRuleException ex)
        {
            var error = ex.RuleName switch
            {
                "OutsideCheckinRadius" => Error.Custom("OUTSIDE_RADIUS", ex.Message),
                "VisitAlreadyClosed"   => Error.Custom("VISIT_CLOSED", ex.Message),
                _                      => Error.Custom(ex.RuleName, ex.Message)
            };
            return Result<CheckoutResponse>.Failure(error);
        }

        // 5. Persistir
        visitRepository.Update(visit);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<CheckoutResponse>.Success(new CheckoutResponse(
            VisitId: visit.Id,
            SellerId: visit.SellerId,
            DistanceMeters: visit.CheckoutDistanceMeters!.Value,
            CheckinTimestamp: visit.CheckinTimestamp,
            CheckoutTimestamp: visit.CheckoutTimestamp!.Value,
            DurationMinutes: visit.DurationMinutes!.Value,
            Message: $"Check-out realizado. Duração da visita: {visit.DurationMinutes} minuto(s)."
        ));
    }
}
