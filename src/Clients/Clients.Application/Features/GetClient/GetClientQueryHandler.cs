using MediatR;
using Microsoft.Extensions.Logging;
using Clients.Application.DTOs;
using Clients.Application.Interfaces;
using Clients.Domain.Exceptions;
using Clients.Domain.Interfaces;

namespace Clients.Application.Features.GetClient;

public sealed class GetClientQueryHandler : IRequestHandler<GetClientQuery, ClientResponseDto>
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientCacheService _cacheService;
    private readonly ILogger<GetClientQueryHandler> _logger;

    public GetClientQueryHandler(
        IClientRepository clientRepository,
        IClientCacheService cacheService,
        ILogger<GetClientQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ClientResponseDto> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        var cached = await _cacheService.GetAsync(request.Id, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT for client {ClientId}", request.Id);
            return cached;
        }

        _logger.LogInformation("Cache MISS for client {ClientId} — fetching from database", request.Id);

        var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new ClientNotFoundException(request.Id);

        var dto = new ClientResponseDto(
            client.Id,
            client.Name,
            client.Email,
            client.Address,
            client.ProfilePictureUrl,
            new BankingDetailsDto(client.BankingDetails.Agency, client.BankingDetails.AccountNumber),
            client.CreatedAt,
            client.UpdatedAt);

        await _cacheService.SetAsync(dto, cancellationToken);

        return dto;
    }
}
