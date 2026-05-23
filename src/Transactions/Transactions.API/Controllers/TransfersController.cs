using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Transactions.Application.DTOs;
using Transactions.Application.Features.CreateTransfer;
using Transactions.Application.Features.GetTransfer;
using Transactions.Application.Features.GetUserTransfers;

namespace Transactions.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates a new transfer between two users.</summary>
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransferCommand(
            request.SenderUserId,
            request.ReceiverUserId,
            request.Amount,
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Gets details of a specific transfer.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTransferQuery(id), cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class UserTransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserTransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Gets all transfers for a specific user.</summary>
    [HttpGet("{userId:guid}/transfers")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(IEnumerable<TransferResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserTransfers(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserTransfersQuery(userId), cancellationToken);
        return Ok(result);
    }
}
