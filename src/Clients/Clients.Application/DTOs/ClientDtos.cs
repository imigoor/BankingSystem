namespace Clients.Application.DTOs;

public sealed record ClientResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Address,
    string? ProfilePictureUrl,
    BankingDetailsDto BankingDetails,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record BankingDetailsDto(string Agency, string AccountNumber);

public sealed record UpdateClientRequest(
    string? Name,
    string? Email,
    string? Address,
    BankingDetailsDto? BankingDetails
);
