namespace Transactions.API.Requests.Transfers;

public sealed record CreateTransferRequest(
    Guid SenderUserId,
    Guid ReceiverUserId,
    decimal Amount,
    string Description
);
