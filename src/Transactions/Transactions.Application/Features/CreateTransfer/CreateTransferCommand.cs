using MediatR;
using Transactions.Application.DTOs;

namespace Transactions.Application.Features.CreateTransfer;

public sealed record CreateTransferCommand(
    Guid SenderUserId,
    Guid ReceiverUserId,
    decimal Amount,
    string Description
) : IRequest<TransferResponseDto>;
