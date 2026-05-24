using MediatR;
using Microsoft.Extensions.Logging;
using Transactions.Application.DTOs;
using Transactions.Application.Interfaces;
using Transactions.Domain.Entities;
using Transactions.Domain.Interfaces;

namespace Transactions.Application.Features.CreateTransfer;

public sealed class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, TransferResponseDto>
{
    private readonly ITransferRepository _transferRepository;
    private readonly IClientServiceClient _clientServiceClient;
    private readonly INotificationService _notificationService;
    private readonly ITransferEventPublisher _eventPublisher;
    private readonly ILogger<CreateTransferCommandHandler> _logger;

    public CreateTransferCommandHandler(
        ITransferRepository transferRepository,
        IClientServiceClient clientServiceClient,
        INotificationService notificationService,
        ITransferEventPublisher eventPublisher,
        ILogger<CreateTransferCommandHandler> logger)
    {
        _transferRepository = transferRepository;
        _clientServiceClient = clientServiceClient;
        _notificationService = notificationService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<TransferResponseDto> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing transfer from {SenderId} to {ReceiverId} amount {Amount}",
            request.SenderUserId, request.ReceiverUserId, request.Amount);

        var senderExists = await _clientServiceClient.ClientExistsAsync(request.SenderUserId, cancellationToken);
        if (!senderExists)
            throw new InvalidOperationException($"Sender client {request.SenderUserId} not found.");

        var receiverExists = await _clientServiceClient.ClientExistsAsync(request.ReceiverUserId, cancellationToken);
        if (!receiverExists)
            throw new InvalidOperationException($"Receiver client {request.ReceiverUserId} not found.");

        var transfer = Transfer.Create(
            request.SenderUserId,
            request.ReceiverUserId,
            request.Amount,
            request.Description);

        await _transferRepository.AddAsync(transfer, cancellationToken);

        transfer.Complete();

        //_transferRepository.UpdateAsync(transfer, cancellationToken);

        await _transferRepository.SaveChangesAsync(cancellationToken);

        await _notificationService.SendTransferNotificationAsync(
            transfer.SenderUserId,
            transfer.ReceiverUserId,
            transfer.Amount,
            cancellationToken);

        _logger.LogInformation("Transfer {TransferId} completed successfully", transfer.Id);

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
