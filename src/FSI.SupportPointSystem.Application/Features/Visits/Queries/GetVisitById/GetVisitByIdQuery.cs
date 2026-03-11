using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Visits.Queries.GetVisitById;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record VisitResponse(
    Guid VisitId,
    Guid SellerId,
    Guid CustomerId,
    // Check-in
    decimal CheckinLatitude,
    decimal CheckinLongitude,
    DateTime CheckinTimestamp,
    double CheckinDistanceMeters,
    // Check-out (nullable)
    decimal? CheckoutLatitude,
    decimal? CheckoutLongitude,
    DateTime? CheckoutTimestamp,
    double? CheckoutDistanceMeters,
    int? DurationMinutes,
    string? CheckoutSummary,
    bool IsOpen,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================================
// Query
// ============================================================
public sealed record GetVisitByIdQuery(Guid VisitId)
    : IRequest<Result<VisitResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetVisitByIdQueryHandler(IVisitRepository visitRepository)
    : IRequestHandler<GetVisitByIdQuery, Result<VisitResponse>>
{
    public async Task<Result<VisitResponse>> Handle(
        GetVisitByIdQuery request,
        CancellationToken cancellationToken)
    {
        var visit = await visitRepository.GetByIdAsync(request.VisitId, cancellationToken);

        if (visit is null)
            return Result<VisitResponse>.Failure(Error.NotFound);

        return Result<VisitResponse>.Success(new VisitResponse(
            VisitId: visit.Id,
            SellerId: visit.SellerId,
            CustomerId: visit.CustomerId,
            CheckinLatitude: visit.CheckinLocation.Latitude,
            CheckinLongitude: visit.CheckinLocation.Longitude,
            CheckinTimestamp: visit.CheckinTimestamp,
            CheckinDistanceMeters: visit.CheckinDistanceMeters,
            CheckoutLatitude: visit.CheckoutLocation?.Latitude,
            CheckoutLongitude: visit.CheckoutLocation?.Longitude,
            CheckoutTimestamp: visit.CheckoutTimestamp,
            CheckoutDistanceMeters: visit.CheckoutDistanceMeters,
            DurationMinutes: visit.DurationMinutes,
            CheckoutSummary: visit.CheckoutSummary,
            IsOpen: visit.IsOpen,
            CreatedAt: visit.CreatedAt,
            UpdatedAt: visit.UpdatedAt
        ));
    }
}
