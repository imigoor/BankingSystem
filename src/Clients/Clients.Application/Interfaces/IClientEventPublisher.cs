namespace Clients.Application.Interfaces;

public interface IClientEventPublisher
{
    Task PublishBankingDataUpdatedAsync(
        Guid clientId,
        string newAgency,
        string newAccountNumber,
        CancellationToken cancellationToken = default);
}
