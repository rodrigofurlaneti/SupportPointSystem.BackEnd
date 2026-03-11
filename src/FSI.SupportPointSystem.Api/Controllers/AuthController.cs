using FSI.SupportPointSystem.Application.Common.Results;
using FSI.SupportPointSystem.Application.Features.Auth.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FSI.SupportPointSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Autentica um usuário (Admin ou Vendedor) via CPF e senha. Retorna JWT válido por 8h.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "UNAUTHORIZED"
                ? Unauthorized(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }
}
