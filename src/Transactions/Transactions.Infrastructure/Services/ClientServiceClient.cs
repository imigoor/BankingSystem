using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Transactions.Application.DTOs;
using Transactions.Application.Interfaces;

namespace Transactions.Infrastructure.Services;

public class ClientServiceClient : IClientServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClientServiceClient> _logger;

    public ClientServiceClient(HttpClient httpClient, ILogger<ClientServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/v1/clients/{clientId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check client existence for {ClientId}", clientId);
            return false;
        }
    }

    public async Task<ClientInfoDto?> GetClientInfoAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ClientInfoDto>(
                $"api/v1/clients/{clientId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get client info for {ClientId}", clientId);
            return null;
        }
    }
}
