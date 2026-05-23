using MediatR;
using Transactions.Application.DTOs;

namespace Transactions.Application.Features.GetTransfer;

public sealed record GetTransferQuery(Guid Id) : IRequest<TransferResponseDto>;
