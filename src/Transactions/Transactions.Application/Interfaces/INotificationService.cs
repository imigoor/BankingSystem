namespace Transactions.Application.Interfaces;

public interface INotificationService
{
    Task SendTransferNotificationAsync(
        Guid senderUserId,
        Guid receiverUserId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
