using Clients.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Clients.Infrastructure.Messaging;

public sealed record ClientBankingDataUpdatedEvent(
    Guid ClientId,
    string NewAgency,
    string NewAccountNumber,
    DateTime UpdatedAt
);

public class ClientEventPublisher : IClientEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ClientEventPublisher> _logger;

    public ClientEventPublisher(IPublishEndpoint publishEndpoint, ILogger<ClientEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishBankingDataUpdatedAsync(Guid clientId, string newAgency,  string newAccountNumber, CancellationToken cancellationToken = default)
    {
        var evt = new ClientBankingDataUpdatedEvent(clientId, newAgency, newAccountNumber, DateTime.UtcNow);
        await _publishEndpoint.Publish(evt, cancellationToken);
        _logger.LogInformation("Published ClientBankingDataUpdatedEvent for client {ClientId}", clientId);
    }
}
