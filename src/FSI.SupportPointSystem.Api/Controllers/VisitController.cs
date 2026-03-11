using FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckin;
using FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckout;
using FSI.SupportPointSystem.Application.Features.Visits.Queries.GetVisitHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FSI.SupportPointSystem.Api.Controllers;

[ApiController]
[Route("api/visits")]
[Authorize]
public sealed class VisitController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Registra check-in do vendedor autenticado.
    /// Valida raio de 100m e bloqueia múltiplos check-ins simultâneos.
    /// </summary>
    [HttpPost("checkin")]
    [Authorize(Roles = "SELLER")]
    [ProducesResponseType(typeof(CheckinResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkin(
        [FromBody] CheckinRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = GetSellerId();
        if (sellerId is null) return Unauthorized();

        var command = new RegisterCheckinCommand(
            SellerId: sellerId.Value,
            CustomerId: request.CustomerId,
            Latitude: request.Latitude,
            Longitude: request.Longitude);

        var result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: response => CreatedAtAction(nameof(GetHistory), new { sellerId = response.SellerId }, response),
            onFailure: error => error.Code switch
            {
                "CONFLICT_CHECKIN"  => Conflict(new { error.Code, error.Description }),
                "OUTSIDE_RADIUS"    => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
                "SELLER_NOT_FOUND"  => NotFound(new { error.Code, error.Description }),
                "CUSTOMER_NOT_FOUND"=> NotFound(new { error.Code, error.Description }),
                _                   => BadRequest(new { error.Code, error.Description })
            });
    }

    /// <summary>
    /// Registra check-out do vendedor autenticado.
    /// Valida raio de 100m e calcula duração da visita.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "SELLER")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = GetSellerId();
        if (sellerId is null) return Unauthorized();

        var command = new RegisterCheckoutCommand(
            SellerId: sellerId.Value,
            Latitude: request.Latitude,
            Longitude: request.Longitude,
            Summary: request.Summary);

        var result = await sender.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "NO_ACTIVE_VISIT" => BadRequest(new { error.Code, error.Description }),
                "OUTSIDE_RADIUS"  => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
                _                 => BadRequest(new { error.Code, error.Description })
            });
    }

    /// <summary>Retorna o histórico paginado de visitas do vendedor autenticado.</summary>
    [HttpGet("history")]
    [Authorize(Roles = "SELLER,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<VisitSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var sellerId = GetSellerId();
        if (sellerId is null) return Unauthorized();

        var query = new GetVisitHistoryQuery(sellerId.Value, page, pageSize);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<IActionResult>(Ok, error => BadRequest(new { error.Code, error.Description }));
    }

    private Guid? GetSellerId()
    {
        var value = User.FindFirstValue("sellerId");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

// ============================================================
// Request bodies (inputs da API - separados dos Commands)
// ============================================================
public sealed record CheckinRequest(Guid CustomerId, decimal Latitude, decimal Longitude);
public sealed record CheckoutRequest(decimal Latitude, decimal Longitude, string? Summary);
