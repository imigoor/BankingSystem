using Microsoft.Extensions.Logging;
using Transactions.Application.Interfaces;

namespace Transactions.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendTransferNotificationAsync(
        Guid senderUserId,
        Guid receiverUserId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NOTIFICATION] Transfer of {Amount:C} from {SenderId} to {ReceiverId} completed.",
            amount, senderUserId, receiverUserId);

        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[EMAIL] Sending email to {To} | Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
