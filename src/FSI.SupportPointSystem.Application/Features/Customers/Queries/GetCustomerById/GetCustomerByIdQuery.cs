using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Customers.Queries.GetCustomerById;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record CustomerResponse(
    Guid CustomerId,
    string CompanyName,
    string Cnpj,
    decimal Latitude,
    decimal Longitude,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================================
// Query
// ============================================================
public sealed record GetCustomerByIdQuery(Guid CustomerId)
    : IRequest<Result<CustomerResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
            return Result<CustomerResponse>.Failure(Error.NotFound);

        return Result<CustomerResponse>.Success(new CustomerResponse(
            CustomerId: customer.Id,
            CompanyName: customer.CompanyName,
            Cnpj: customer.Cnpj.Formatted,
            Latitude: customer.LocationTarget.Latitude,
            Longitude: customer.LocationTarget.Longitude,
            IsActive: customer.IsActive,
            CreatedAt: customer.CreatedAt,
            UpdatedAt: customer.UpdatedAt
        ));
    }
}
