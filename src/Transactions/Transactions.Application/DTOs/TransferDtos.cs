namespace Transactions.Application.DTOs;

public sealed record TransferResponseDto(
    Guid Id,
    Guid SenderUserId,
    Guid ReceiverUserId,
    decimal Amount,
    string Description,
    string Status,
    DateTime CreatedAt
);