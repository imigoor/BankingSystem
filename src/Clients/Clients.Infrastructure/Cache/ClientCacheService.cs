using System.Text.Json;
using Clients.Application.DTOs;
using Clients.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Clients.Infrastructure.Cache;

public class ClientCacheService : IClientCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<ClientCacheService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    public ClientCacheService(IDistributedCache cache, ILogger<ClientCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    private static string CacheKey(Guid clientId) => $"client:{clientId}";

    public async Task<ClientResponseDto?> GetAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(CacheKey(clientId), cancellationToken);
            return data is null ? null : JsonSerializer.Deserialize<ClientResponseDto>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for client {ClientId} — falling through to database", clientId);
            return null;
        }
    }

    public async Task SetAsync(ClientResponseDto client, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(client);
            await _cache.SetStringAsync(CacheKey(client.Id), json, CacheOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for client {ClientId}", client.Id);
        }
    }

    public async Task InvalidateAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(CacheKey(clientId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for client {ClientId}", clientId);
        }
    }
}
