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
