using Clients.Application.DTOs;

namespace Clients.API.Requests.Client;

public sealed record UpdateClientRequest(
    string? Name,
    string? Email,
    string? Address,
    BankingDetailsDto? BankingDetails
);
