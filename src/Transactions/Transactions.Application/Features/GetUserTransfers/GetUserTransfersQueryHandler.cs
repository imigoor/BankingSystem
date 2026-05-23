using MediatR;
using Transactions.Application.DTOs;
using Transactions.Domain.Interfaces;

namespace Transactions.Application.Features.GetUserTransfers;

public sealed class GetUserTransfersQueryHandler : IRequestHandler<GetUserTransfersQuery, IEnumerable<TransferResponseDto>>
{
    private readonly ITransferRepository _transferRepository;

    public GetUserTransfersQueryHandler(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<IEnumerable<TransferResponseDto>> Handle(GetUserTransfersQuery request, CancellationToken cancellationToken)
    {
        var transfers = await _transferRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return transfers.Select(t => new TransferResponseDto(
            t.Id,
            t.SenderUserId,
            t.ReceiverUserId,
            t.Amount,
            t.Description,
            t.Status.ToString(),
            t.CreatedAt));
    }
}
