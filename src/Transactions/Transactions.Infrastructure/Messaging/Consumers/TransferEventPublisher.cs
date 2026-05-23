using MassTransit;
using Transactions.Application.Interfaces;

namespace Transactions.Infrastructure.Messaging;

public class TransferEventPublisher : ITransferEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public TransferEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishTransferCompletedAsync(Guid transferId, Guid senderUserId, Guid receiverUserId, decimal amount, CancellationToken cancellationToken)
    {
        await _publishEndpoint.Publish(new
        {
            TransferId = transferId,
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            Amount = amount,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}