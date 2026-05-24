using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Transactions.Application.DTOs;
using Transactions.Application.Features.GetUserTransfers;

namespace Transactions.API.Controllers;

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

    /// <summary> Retrieves the transaction history (statement) for a specific user, including both sent and received transfers. </summary>
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
