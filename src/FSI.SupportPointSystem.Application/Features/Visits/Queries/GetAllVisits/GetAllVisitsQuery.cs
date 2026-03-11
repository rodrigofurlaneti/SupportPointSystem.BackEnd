using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Application.Features.Visits.Queries.GetVisitById;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Visits.Queries.GetAllVisits;

// ============================================================
// Query
// ============================================================
/// <summary>
/// Retorna todas as visitas paginadas. Apenas ADMIN pode executar.
/// Para consulta por vendedor, use GetVisitHistoryQuery.
/// </summary>
public sealed record GetAllVisitsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<VisitResponse>>>;

// ============================================================
// Validator
// ============================================================
public sealed class GetAllVisitsQueryValidator : AbstractValidator<GetAllVisitsQuery>
{
    public GetAllVisitsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Página deve ser maior que zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Tamanho de página deve estar entre 1 e 100.");
    }
}

// ============================================================
// Handler
// ============================================================
public sealed class GetAllVisitsQueryHandler(IVisitRepository visitRepository)
    : IRequestHandler<GetAllVisitsQuery, Result<IReadOnlyList<VisitResponse>>>
{
    public async Task<Result<IReadOnlyList<VisitResponse>>> Handle(
        GetAllVisitsQuery request,
        CancellationToken cancellationToken)
    {
        var visits = await visitRepository.GetAllAsync(request.Page, request.PageSize, cancellationToken);

        var response = visits
            .Select(v => new VisitResponse(
                VisitId: v.Id,
                SellerId: v.SellerId,
                CustomerId: v.CustomerId,
                CheckinLatitude: v.CheckinLocation.Latitude,
                CheckinLongitude: v.CheckinLocation.Longitude,
                CheckinTimestamp: v.CheckinTimestamp,
                CheckinDistanceMeters: v.CheckinDistanceMeters,
                CheckoutLatitude: v.CheckoutLocation?.Latitude,
                CheckoutLongitude: v.CheckoutLocation?.Longitude,
                CheckoutTimestamp: v.CheckoutTimestamp,
                CheckoutDistanceMeters: v.CheckoutDistanceMeters,
                DurationMinutes: v.DurationMinutes,
                CheckoutSummary: v.CheckoutSummary,
                IsOpen: v.IsOpen,
                CreatedAt: v.CreatedAt,
                UpdatedAt: v.UpdatedAt
            ))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<VisitResponse>>.Success(response);
    }
}
