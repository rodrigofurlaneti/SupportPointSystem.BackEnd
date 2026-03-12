using FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckin;
using FSI.SupportPointSystem.Application.Features.Visits.Commands.RegisterCheckout;
using FSI.SupportPointSystem.Application.Features.Visits.Queries.GetAllVisits;
using FSI.SupportPointSystem.Application.Features.Visits.Queries.GetVisitById;
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
    /// Retorna todas as visitas paginadas. Apenas ADMIN.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<VisitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllVisitsQuery(page, pageSize), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>
    /// Retorna uma visita pelo Id. Apenas ADMIN.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVisitByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

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
            onSuccess: response => CreatedAtAction(nameof(GetById), new { id = response.VisitId }, response),
            onFailure: error => error.Code switch
            {
                "CONFLICT_CHECKIN"   => Conflict(new { error.Code, error.Description }),
                "OUTSIDE_RADIUS"     => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
                "SELLER_NOT_FOUND"   => NotFound(new { error.Code, error.Description }),
                "CUSTOMER_NOT_FOUND" => NotFound(new { error.Code, error.Description }),
                _                    => BadRequest(new { error.Code, error.Description })
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
    /// <summary>
    /// Retorna o histórico paginado. 
    /// SELLER: Vê apenas o seu. 
    /// ADMIN: Vê de todos ou filtra por um vendedor específico.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = "SELLER,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<VisitSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? sellerId, // Novo: permite passar o ID via query string
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid? targetId;

        if (User.IsInRole("ADMIN"))
        {
            // Se for Admin, usa o ID passado no front ou null para pegar todos
            targetId = sellerId;
        }
        else
        {
            // Se for Seller, ignora o que veio do front e força o ID do próprio token (Segurança)
            targetId = GetSellerId();
            if (targetId is null) return Unauthorized(new { Code = "AUTH_ERROR", Description = "SellerId não encontrado no token." });
        }

        // Ajuste sua GetVisitHistoryQuery para aceitar Guid? (opcional) 
        // ou garanta que o Handler trate o Guid.Empty / Null como "trazer todos"
        var query = new GetVisitHistoryQuery(targetId, page, pageSize);
        var result = await sender.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Description }));
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
