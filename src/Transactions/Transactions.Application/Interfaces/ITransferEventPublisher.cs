namespace Transactions.Application.Interfaces;

public interface ITransferEventPublisher
{
    Task PublishTransferCompletedAsync(Guid transferId, Guid senderUserId, Guid receiverUserId, decimal amount, CancellationToken cancellationToken = default);
}
