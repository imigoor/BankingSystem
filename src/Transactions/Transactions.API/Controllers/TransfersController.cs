using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Transactions.API.Requests.Transfers;
using Transactions.Application.DTOs;
using Transactions.Application.Features.CreateTransfer;
using Transactions.Application.Features.GetTransfer;

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

    /// <summary> Initiates a new financial transfer between a sender and a receiver. </summary>
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

    /// <summary> Retrieves the complete details of a specific transfer by its unique identifier. Restricted to Admin role for auditing. </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TransferResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTransferQuery(id), cancellationToken);
        return Ok(result);
    }
}