using MassTransit;
using Microsoft.Extensions.Logging;

namespace Transactions.Infrastructure.Messaging.Consumers;

public sealed record ClientBankingDataUpdatedEvent(
    Guid ClientId,
    string NewAgency,
    string NewAccountNumber,
    DateTime UpdatedAt
);


public sealed class ClientBankingDataUpdatedConsumer : IConsumer<ClientBankingDataUpdatedEvent>
{
    private readonly ILogger<ClientBankingDataUpdatedConsumer> _logger;

    public ClientBankingDataUpdatedConsumer(ILogger<ClientBankingDataUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ClientBankingDataUpdatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "Received ClientBankingDataUpdated event for ClientId={ClientId}. New Agency={Agency}, Account={Account}",
            evt.ClientId, evt.NewAgency, evt.NewAccountNumber);

        return Task.CompletedTask;
    }
}
