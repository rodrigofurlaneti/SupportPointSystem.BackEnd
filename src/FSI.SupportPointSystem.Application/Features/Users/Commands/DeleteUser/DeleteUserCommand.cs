using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using FSI.SupportPointSystem.Domain.Interfaces.Services;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Users.Commands.DeleteUser;

// ============================================================
// DTO de resposta
// ============================================================
public sealed record DeleteUserResponse(
    Guid UserId,
    string Message
);

// ============================================================
// Command
// ============================================================
/// <summary>
/// Remove um usuário do sistema.
/// Regra: usuário com vendedor ativo não pode ser removido diretamente.
/// Apenas ADMIN pode executar.
/// </summary>
public sealed record DeleteUserCommand(Guid UserId)
    : IRequest<Result<DeleteUserResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class DeleteUserCommandHandler(
    IUserRepository userRepository,
    ISellerRepository sellerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteUserCommand, Result<DeleteUserResponse>>
{
    public async Task<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<DeleteUserResponse>.Failure(Error.NotFound);

        // Verifica se o usuário tem um vendedor ativo vinculado
        var linkedSeller = await sellerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (linkedSeller is not null && linkedSeller.IsActive)
            return Result<DeleteUserResponse>.Failure(
                Error.Custom("USER_HAS_ACTIVE_SELLER",
                    "Não é possível remover um usuário com perfil de vendedor ativo. Desative o vendedor primeiro."));

        userRepository.Remove(user);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result<DeleteUserResponse>.Success(new DeleteUserResponse(
            UserId: user.Id,
            Message: "Usuário removido com sucesso."
        ));
    }
}
