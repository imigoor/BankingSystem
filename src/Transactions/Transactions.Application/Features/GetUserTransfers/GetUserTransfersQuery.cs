using MediatR;
using Transactions.Application.DTOs;

namespace Transactions.Application.Features.GetUserTransfers;

public sealed record GetUserTransfersQuery(Guid UserId) : IRequest<IEnumerable<TransferResponseDto>>;
