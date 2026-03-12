using FSI.SupportPointSystem.Application.Features.Customers.Commands.DeleteCustomer;
using FSI.SupportPointSystem.Application.Features.Customers.Commands.UpsertCustomer;
using FSI.SupportPointSystem.Application.Features.Customers.Queries.GetAllCustomers;
using FSI.SupportPointSystem.Application.Features.Customers.Queries.GetCustomerById;
using FSI.SupportPointSystem.Application.Features.Sellers.Commands.CreateSeller;
using FSI.SupportPointSystem.Application.Features.Sellers.Commands.DeleteSeller;
using FSI.SupportPointSystem.Application.Features.Sellers.Commands.UpdateSeller;
using FSI.SupportPointSystem.Application.Features.Sellers.Queries.GetAllSellers;
using FSI.SupportPointSystem.Application.Features.Sellers.Queries.GetSellerById;
using FSI.SupportPointSystem.Application.Features.Users.Commands.DeleteUser;
using FSI.SupportPointSystem.Application.Features.Users.Commands.UpdateUser;
using FSI.SupportPointSystem.Application.Features.Users.Queries.GetAllUsers;
using FSI.SupportPointSystem.Application.Features.Users.Queries.GetUserById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FSI.SupportPointSystem.Api.Controllers;

// ============================================================
// SellerController - CRUD completo via CQRS
// ============================================================
/// <summary>Gestão de vendedores - apenas ADMIN.</summary>
[ApiController]
[Route("api/sellers")]
[Authorize(Roles = "ADMIN")]
public sealed class SellerController(ISender sender) : ControllerBase
{
    /// <summary>Retorna todos os vendedores ativos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SellerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllSellersQuery(), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Retorna um vendedor pelo Id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SellerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSellerByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

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
            onSuccess: response => CreatedAtAction(nameof(GetById), new { id = response.SellerId }, response),
            onFailure: error => error.Code == "CPF_ALREADY_EXISTS"
                ? Conflict(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Atualiza perfil e status (ativo/inativo) de um vendedor.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UpdateSellerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSellerRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSellerCommand(id, body.Name, body.Phone, body.Email, body.IsActive);
        var result = await sender.Send(command, cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Remove (desativa) um vendedor. Bloqueia se houver visita ativa.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(DeleteSellerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteSellerCommand(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "NOT_FOUND" => NotFound(new { error.Code, error.Description }),
                "SELLER_HAS_ACTIVE_VISIT" => Conflict(new { error.Code, error.Description }),
                _ => BadRequest(new { error.Code, error.Description })
            });
    }
}

/// <summary>Body para PUT /api/sellers/{id}</summary>
public sealed record UpdateSellerRequest(string Name, string? Phone, string? Email, bool IsActive);

// ============================================================
// CustomerController - CRUD completo via CQRS
// ============================================================
/// <summary>Gestão de clientes - Acesso ADMIN e SELLER (limitado).</summary>
[ApiController]
[Route("api/customers")]
[Authorize(Roles = "ADMIN")] // Padrão para a controller toda é ADMIN
public sealed class CustomerController(ISender sender) : ControllerBase
{
    /// <summary>Retorna todos os clientes ativos - Aberto para ADMIN e SELLER.</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN,SELLER")] // Sobrescreve permitindo ambas as roles
    [ProducesResponseType(typeof(IReadOnlyList<CustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllCustomersQuery(), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Retorna um cliente pelo Id - Aberto para ADMIN e SELLER.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN,SELLER")] // Sobrescreve permitindo ambas as roles
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Cria ou atualiza um cliente - Apenas ADMIN (conforme o atributo da classe).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpsertCustomerResponse), StatusCodes.Status201Created)]
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
                ? CreatedAtAction(nameof(GetById), new { id = response.CustomerId }, response)
                : Ok(response),
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Remove um cliente - Apenas ADMIN.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(DeleteCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteCustomerCommand(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }
}

// ============================================================
// UserController - CRUD completo via CQRS
// ============================================================
/// <summary>Gestão de usuários - apenas ADMIN.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = "ADMIN")]
public sealed class UserController(ISender sender) : ControllerBase
{
    /// <summary>Retorna todos os usuários do sistema.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllUsersQuery(), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Retorna um usuário pelo Id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Atualiza a senha de um usuário.</summary>
    [HttpPut("{id:guid}/password")]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePassword(
        Guid id,
        [FromBody] UpdatePasswordRequest body,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserCommand(id, body.NewPassword), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code == "NOT_FOUND"
                ? NotFound(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description }));
    }

    /// <summary>Remove um usuário. Bloqueado se houver vendedor ativo vinculado.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(DeleteUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return result.Match<IActionResult>(
            onSuccess: Ok,
            onFailure: error => error.Code switch
            {
                "NOT_FOUND" => NotFound(new { error.Code, error.Description }),
                "USER_HAS_ACTIVE_SELLER" => Conflict(new { error.Code, error.Description }),
                _ => BadRequest(new { error.Code, error.Description })
            });
    }
}

/// <summary>Body para PUT /api/users/{id}/password</summary>
public sealed record UpdatePasswordRequest(string NewPassword);
