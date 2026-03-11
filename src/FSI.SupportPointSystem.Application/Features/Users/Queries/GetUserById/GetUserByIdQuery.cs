using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Domain.Entities;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Users.Queries.GetUserById;

// ============================================================
// DTO de resposta (sem expor PasswordHash)
// ============================================================
public sealed record UserResponse(
    Guid UserId,
    string Cpf,
    string Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ============================================================
// Query
// ============================================================
public sealed record GetUserByIdQuery(Guid UserId)
    : IRequest<Result<UserResponse>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<UserResponse>.Failure(Error.NotFound);

        return Result<UserResponse>.Success(new UserResponse(
            UserId: user.Id,
            Cpf: user.Cpf.Formatted,
            Role: user.Role.ToString(),
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt
        ));
    }
}
