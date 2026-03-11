using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Application.Features.Customers.Queries.GetCustomerById;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Customers.Queries.GetAllCustomers;

// ============================================================
// Query
// ============================================================
/// <summary>
/// Retorna todos os clientes ativos. Filtragem por inativos via parâmetro.
/// </summary>
public sealed record GetAllCustomersQuery(bool OnlyActive = true)
    : IRequest<Result<IReadOnlyList<CustomerResponse>>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetAllCustomersQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetAllCustomersQuery, Result<IReadOnlyList<CustomerResponse>>>
{
    public async Task<Result<IReadOnlyList<CustomerResponse>>> Handle(
        GetAllCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllActiveAsync(cancellationToken);

        var response = customers
            .Select(c => new CustomerResponse(
                CustomerId: c.Id,
                CompanyName: c.CompanyName,
                Cnpj: c.Cnpj.Formatted,
                Latitude: c.LocationTarget.Latitude,
                Longitude: c.LocationTarget.Longitude,
                IsActive: c.IsActive,
                CreatedAt: c.CreatedAt,
                UpdatedAt: c.UpdatedAt
            ))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<CustomerResponse>>.Success(response);
    }
}
