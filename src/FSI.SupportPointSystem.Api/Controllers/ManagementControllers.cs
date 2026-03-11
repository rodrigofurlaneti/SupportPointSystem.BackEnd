using FSI.SupportPointSystem.Application.Features.Customers.Commands.UpsertCustomer;
using FSI.SupportPointSystem.Application.Features.Sellers.Commands.CreateSeller;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FSI.SupportPointSystem.Api.Controllers;

/// <summary>Gestão de vendedores - apenas ADMIN.</summary>
[ApiController]
[Route("api/sellers")]
[Authorize(Roles = "ADMIN")]
public sealed class SellerController(ISender sender) : ControllerBase
{
    /// <summary>Cadastra um novo vendedor. Cria User + Seller em transação única.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSellerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSellerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: response => CreatedAtAction(nameof(Create), new { id = response.SellerId }, response),
            onFailure: error => error.Code == "CPF_ALREADY_EXISTS"
                ? Conflict(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }
}

/// <summary>Gestão de clientes - apenas ADMIN.</summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = "ADMIN")]
public sealed class CustomerController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Cria ou atualiza um cliente pelo CNPJ (upsert).
    /// Coordenadas alvo são obrigatórias para validação de proximidade.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpsertCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: response => response.IsNew
                ? StatusCode(StatusCodes.Status201Created, response)
                : Ok(response),
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }
}
