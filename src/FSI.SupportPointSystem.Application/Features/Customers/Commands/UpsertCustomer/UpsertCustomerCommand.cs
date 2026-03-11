using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Customers.Commands.UpsertCustomer;

public sealed record UpsertCustomerResponse(
    Guid CustomerId,
    string CompanyName,
    string Cnpj,
    decimal Latitude,
    decimal Longitude,
    bool IsNew,
    string Message
);

public sealed record UpsertCustomerCommand(
    string CompanyName,
    string Cnpj,
    decimal Latitude,
    decimal Longitude
) : IRequest<Result<UpsertCustomerResponse>>;

public sealed class UpsertCustomerCommandValidator : AbstractValidator<UpsertCustomerCommand>
{
    public UpsertCustomerCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Razão social é obrigatória.")
            .MaximumLength(150).WithMessage("Razão social deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório.")
            .Length(14, 18).WithMessage("CNPJ deve ter entre 14 e 18 caracteres.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m)
            .WithMessage("Latitude deve estar entre -90 e 90 graus.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m)
            .WithMessage("Longitude deve estar entre -180 e 180 graus.");
    }
}

public sealed class UpsertCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertCustomerCommand, Result<UpsertCustomerResponse>>
{
    public async Task<Result<UpsertCustomerResponse>> Handle(
        UpsertCustomerCommand request,
        CancellationToken cancellationToken)
    {
        Cnpj cnpj;
        try
        {
            cnpj = Cnpj.Create(request.Cnpj);
        }
        catch (Domain.Exceptions.DomainValidationException ex)
        {
            return Result<UpsertCustomerResponse>.Failure(Error.Custom("INVALID_CNPJ", ex.Message));
        }

        var existing = await customerRepository.GetByCnpjAsync(cnpj, cancellationToken);
        bool isNew;
        Customer customer;

        if (existing is not null)
        {
            // Atualizar cliente existente
            existing.UpdateCompanyName(request.CompanyName);
            existing.UpdateLocation(request.Latitude, request.Longitude);
            customerRepository.Update(existing);
            customer = existing;
            isNew = false;
        }
        else
        {
            // Criar novo cliente
            try
            {
                customer = Customer.Create(request.CompanyName, request.Cnpj, request.Latitude, request.Longitude);
            }
            catch (Domain.Exceptions.DomainValidationException ex)
            {
                return Result<UpsertCustomerResponse>.Failure(Error.Custom("DOMAIN_VALIDATION", ex.Message));
            }
            await customerRepository.AddAsync(customer, cancellationToken);
            isNew = true;
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return Result<UpsertCustomerResponse>.Success(new UpsertCustomerResponse(
            CustomerId: customer.Id,
            CompanyName: customer.CompanyName,
            Cnpj: customer.Cnpj.Formatted,
            Latitude: customer.LocationTarget.Latitude,
            Longitude: customer.LocationTarget.Longitude,
            IsNew: isNew,
            Message: isNew ? "Cliente criado com sucesso." : "Cliente atualizado com sucesso."
        ));
    }
}
