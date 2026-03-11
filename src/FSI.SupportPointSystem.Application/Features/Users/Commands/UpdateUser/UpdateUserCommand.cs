using FluentValidation;
using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Users.Commands.UpdateUser;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record UpdateUserResponse(
    Guid UserId,
    string Message
);

// ============================================================
// Command
// ============================================================
/// <summary>
/// Atualiza a senha de um usuário.
/// Apenas ADMIN pode redefinir senhas de outros usuários.
/// </summary>
public sealed record UpdateUserCommand(
    Guid UserId,
    string NewPassword
) : IRequest<Result<UpdateUserResponse>>;

// ============================================================
// Validator
// ============================================================
public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Id do usuário é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.");
    }
}

// ============================================================
// Handler
// ============================================================
public sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
{
    public async Task<Result<UpdateUserResponse>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<UpdateUserResponse>.Failure(Error.NotFound);

        try
        {
            var newHash = passwordHasher.Hash(request.NewPassword);
            user.UpdatePasswordHash(newHash);
        }
        catch (Domain.Exceptions.DomainValidationException ex)
        {
            return Result<UpdateUserResponse>.Failure(Error.Custom("DOMAIN_VALIDATION", ex.Message));
        }

        userRepository.Update(user);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<UpdateUserResponse>.Success(new UpdateUserResponse(
            UserId: user.Id,
            Message: "Senha atualizada com sucesso."
        ));
    }
}
