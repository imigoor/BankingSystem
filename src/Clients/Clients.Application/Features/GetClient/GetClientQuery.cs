using MediatR;
using Clients.Application.DTOs;

namespace Clients.Application.Features.GetClient;

public sealed record GetClientQuery(Guid Id) : IRequest<ClientResponseDto>;
