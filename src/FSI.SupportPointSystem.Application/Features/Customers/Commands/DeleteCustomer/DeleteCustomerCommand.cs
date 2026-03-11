using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Customers.Commands.DeleteCustomer;

// ============================================================
// Command
// ============================================================
/// <summary>
/// Remove (soft delete via desativação) um cliente pelo Id.
/// Apenas ADMIN pode executar.
/// </summary>
public sealed record DeleteCustomerCommand(Guid CustomerId)
    : IRequest<Result<DeleteCustomerResponse>>;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record DeleteCustomerResponse(
    Guid CustomerId,
    string Message
);

// ============================================================
// Handler
// ============================================================
public sealed class DeleteCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCustomerCommand, Result<DeleteCustomerResponse>>
{
    public async Task<Result<DeleteCustomerResponse>> Handle(
        DeleteCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
            return Result<DeleteCustomerResponse>.Failure(Error.NotFound);

        customerRepository.Remove(customer);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<DeleteCustomerResponse>.Success(new DeleteCustomerResponse(
            CustomerId: customer.Id,
            Message: "Cliente removido com sucesso."
        ));
    }
}
