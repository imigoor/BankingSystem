using Asp.Versioning;
using Clients.Application.DTOs;
using Clients.Application.Features.GetClient;
using Clients.Application.Features.UpdateClient;
using Clients.Application.Features.UpdateProfilePicture;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clients.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clients")]
[Authorize]
public sealed class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientsController(IMediator mediator)
        => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClientQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(ClientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateClientCommand(
            id,
            request.Name,
            request.Email,
            request.Address,
            request.BankingDetails);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/profile-picture")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfilePicture(Guid id, IFormFile profilePicture, CancellationToken cancellationToken)
    {
        if (profilePicture is null || profilePicture.Length == 0)
            return BadRequest(new { message = "Profile picture file is required." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(profilePicture.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG and WEBP images are allowed." });

        const long maxSize = 5 * 1024 * 1024;
        if (profilePicture.Length > maxSize)
            return BadRequest(new { message = "File size cannot exceed 5 MB." });

        await using var stream = profilePicture.OpenReadStream();

        var command = new UpdateProfilePictureCommand(
            id,
            stream,
            profilePicture.FileName,
            profilePicture.ContentType);

        var url = await _mediator.Send(command, cancellationToken);
        return Ok(new { profilePictureUrl = url });
    }
}
