namespace FSI.SupportPointSystem.Application.Features.Auth.Dtos
{
    public sealed record LoginResponse(
        string Token,
        string UserRole,
        Guid UserId,
        Guid? SellerId,
        DateTime ExpiresAt
    );
}
