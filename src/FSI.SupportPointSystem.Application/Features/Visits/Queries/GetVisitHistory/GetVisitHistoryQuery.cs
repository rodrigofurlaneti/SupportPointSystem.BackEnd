using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Visits.Queries.GetVisitHistory;

public sealed record VisitSummaryDto(
    Guid VisitId,
    Guid CustomerId,
    string CustomerName,
    string SellerName, 
    decimal CheckinLatitude,
    decimal CheckinLongitude,
    double CheckinDistanceMeters,
    DateTime CheckinTimestamp,
    bool IsOpen,
    DateTime? CheckoutTimestamp,
    double? CheckoutDistanceMeters,
    int? DurationMinutes,
    string? CheckoutSummary
);

public sealed record GetVisitHistoryQuery(
    Guid? SellerId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<IReadOnlyList<VisitSummaryDto>>>;

public sealed class GetVisitHistoryQueryValidator : AbstractValidator<GetVisitHistoryQuery>
{
    public GetVisitHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetVisitHistoryQueryHandler(IVisitRepository visitRepository)
    : IRequestHandler<GetVisitHistoryQuery, Result<IReadOnlyList<VisitSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<VisitSummaryDto>>> Handle(
        GetVisitHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var visits = await visitRepository.GetVisitHistoryAsync(
            request.SellerId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = visits.Select(MapToDto).ToList().AsReadOnly();
        return Result<IReadOnlyList<VisitSummaryDto>>.Success(dtos);
    }
    private static VisitSummaryDto MapToDto(Visit v) => new(
            VisitId: v.Id,
            CustomerId: v.CustomerId,
            CustomerName: v.Customer?.CompanyName ?? "Cliente Desconhecido",
            SellerName: v.Seller?.Name ?? "Vendedor Desconhecido",
            CheckinLatitude: v.CheckinLocation.Latitude,
            CheckinLongitude: v.CheckinLocation.Longitude,
            CheckinDistanceMeters: v.CheckinDistanceMeters,
            CheckinTimestamp: v.CheckinTimestamp,
            IsOpen: v.IsOpen,
            CheckoutTimestamp: v.CheckoutTimestamp,
            CheckoutDistanceMeters: v.CheckoutDistanceMeters,
            DurationMinutes: v.DurationMinutes,
            CheckoutSummary: v.CheckoutSummary
        );
}