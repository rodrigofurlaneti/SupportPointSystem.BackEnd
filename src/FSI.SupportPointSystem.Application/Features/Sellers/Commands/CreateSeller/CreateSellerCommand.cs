using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Sellers.Commands.CreateSeller;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record CreateSellerResponse(
    Guid SellerId,
    Guid UserId,
    string Name,
    string Cpf,
    string Message
);

// ============================================================
// Command
// ============================================================
/// <summary>
/// Command para cadastro de novo vendedor. Apenas ADMIN pode executar.
/// Cria User (credenciais) + Seller (perfil) em uma única transação.
/// </summary>
public sealed record CreateSellerCommand(
    string Cpf,
    string Password,
    string Name,
    string? Phone,
    string? Email
) : IRequest<Result<CreateSellerResponse>>;

// ============================================================
// Validator
// ============================================================
public sealed class CreateSellerCommandValidator : AbstractValidator<CreateSellerCommand>
{
    public CreateSellerCommandValidator()
    {
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.")
            .Length(11, 14).WithMessage("CPF deve ter entre 11 e 14 caracteres (com ou sem formatação).");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.");

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
public sealed class CreateSellerCommandHandler(
    IUserRepository userRepository,
    ISellerRepository sellerRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateSellerCommand, Result<CreateSellerResponse>>
{
    public async Task<Result<CreateSellerResponse>> Handle(
        CreateSellerCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validar e criar Value Object CPF
        Domain.ValueObjects.Cpf cpf;
        try
        {
            cpf = Domain.ValueObjects.Cpf.Create(request.Cpf);
        }
        catch (Domain.Exceptions.DomainValidationException ex)
        {
            return Result<CreateSellerResponse>.Failure(Error.Custom("INVALID_CPF", ex.Message));
        }

        // 2. CPF deve ser único
        var cpfExists = await userRepository.ExistsByCpfAsync(cpf, cancellationToken);
        if (cpfExists)
            return Result<CreateSellerResponse>.Failure(
                Error.Custom("CPF_ALREADY_EXISTS", $"Já existe um usuário com o CPF informado."));

        // 3. Criar credenciais
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.CreateSeller(cpf, passwordHash);

        // 4. Criar perfil de vendedor
        var seller = Seller.Create(user, request.Name, request.Phone, request.Email);

        // 5. Persistir em transação única (Unit of Work)
        await userRepository.AddAsync(user, cancellationToken);
        await sellerRepository.AddAsync(seller, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<CreateSellerResponse>.Success(new CreateSellerResponse(
            SellerId: seller.Id,
            UserId: user.Id,
            Name: seller.Name,
            Cpf: cpf.Formatted,
            Message: "Vendedor cadastrado com sucesso."
        ));
    }
}
