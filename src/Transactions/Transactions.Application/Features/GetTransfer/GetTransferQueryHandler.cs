using MediatR;
using Transactions.Application.DTOs;
using Transactions.Domain.Exceptions;
using Transactions.Domain.Interfaces;

namespace Transactions.Application.Features.GetTransfer;

public sealed class GetTransferQueryHandler : IRequestHandler<GetTransferQuery, TransferResponseDto>
{
    private readonly ITransferRepository _transferRepository;

    public GetTransferQueryHandler(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<TransferResponseDto> Handle(GetTransferQuery request, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new TransferNotFoundException(request.Id);

        return new TransferResponseDto(
            transfer.Id,
            transfer.SenderUserId,
            transfer.ReceiverUserId,
            transfer.Amount,
            transfer.Description,
            transfer.Status.ToString(),
            transfer.CreatedAt);
    }
}
