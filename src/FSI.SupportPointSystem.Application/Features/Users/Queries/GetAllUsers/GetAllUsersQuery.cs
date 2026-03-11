using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Application.Features.Users.Queries.GetUserById;
using FSI.SupportPointSystem.Domain.Interfaces.Repositories;
using MediatR;

namespace FSI.SupportPointSystem.Application.Features.Users.Queries.GetAllUsers;

// ============================================================
// Query
// ============================================================
/// <summary>
/// Retorna todos os usuários do sistema. Apenas ADMIN pode executar.
/// </summary>
public sealed record GetAllUsersQuery()
    : IRequest<Result<IReadOnlyList<UserResponse>>>;

// ============================================================
// Handler
// ============================================================
public sealed class GetAllUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersQuery, Result<IReadOnlyList<UserResponse>>>
{
    public async Task<Result<IReadOnlyList<UserResponse>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);

        var response = users
            .Select(u => new UserResponse(
                UserId: u.Id,
                Cpf: u.Cpf.Formatted,
                Role: u.Role.ToString(),
                CreatedAt: u.CreatedAt,
                UpdatedAt: u.UpdatedAt
            ))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<UserResponse>>.Success(response);
    }
}
