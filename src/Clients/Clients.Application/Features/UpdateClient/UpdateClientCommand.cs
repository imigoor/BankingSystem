using MediatR;
using Clients.Application.DTOs;

namespace Clients.Application.Features.UpdateClient;

public sealed record UpdateClientCommand(
    Guid ClientId,
    string? Name,
    string? Email,
    string? Address,
    BankingDetailsDto? BankingDetails
) : IRequest<ClientResponseDto>;
