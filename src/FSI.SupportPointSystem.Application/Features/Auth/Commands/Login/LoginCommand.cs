using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using FSI.SupportPointSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSI.SupportPointSystem.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    string Token,
    string UserRole,
    Guid UserId,
    Guid? SellerId,
    DateTime ExpiresAt
);

public sealed record LoginCommand(
    string Cpf,
    string Password
) : IRequest<Result<LoginResponse>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF é obrigatório.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.");
    }
}

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ISellerRepository sellerRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Montar CPF Value Object
        Domain.ValueObjects.Cpf cpf;
        try
        {
            cpf = Domain.ValueObjects.Cpf.Create(request.Cpf);
        }
        catch
        {
            // CPF estruturalmente inválido = credenciais inválidas (não revelar detalhe)
            return Result<LoginResponse>.Failure(Error.Unauthorized);
        }

        // 2. Buscar usuário
        var user = await userRepository.GetByCpfAsync(cpf, cancellationToken);
        if (user is null)
            return Result<LoginResponse>.Failure(Error.Unauthorized);

        // 3. Verificar senha
        // LOG DE DIAGNÓSTICO (Insira antes do if da senha)
        Console.WriteLine($"[DEBUG] Senha Digitada: '{request.Password}'");
        Console.WriteLine($"[DEBUG] Tamanho Senha: {request.Password.Length}");
        Console.WriteLine($"[DEBUG] Hash no Banco: '{user.PasswordHash}'");
        Console.WriteLine($"[DEBUG] Tamanho Hash: {user.PasswordHash.Length}");

        // Teste de sanidade: gerar um hash na hora e comparar
        var hashLocal = passwordHasher.Hash(request.Password);
        Console.WriteLine($"[DEBUG] Hash gerado agora: {hashLocal}");
        Console.WriteLine($"[DEBUG] Verify Local: {passwordHasher.Verify(request.Password, hashLocal)}");
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure(Error.Unauthorized);

        // 4. Carregar perfil de vendedor (se for vendedor)
        var seller = user.IsSeller
            ? await sellerRepository.GetByUserIdAsync(user.Id, cancellationToken)
            : null;

        if (user.IsSeller && (seller is null || !seller.IsActive))
            return Result<LoginResponse>.Failure(
                Error.Custom("SELLER_INACTIVE", "Vendedor está inativo. Contate o administrador."));

        // 5. Gerar token JWT (8h de validade conforme regra de negócio)
        var token = tokenService.GenerateToken(user, seller);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return Result<LoginResponse>.Success(new LoginResponse(
            Token: token,
            UserRole: user.Role.ToString().ToUpperInvariant(),
            UserId: user.Id,
            SellerId: seller?.Id,
            ExpiresAt: expiresAt
        ));
    }
}
