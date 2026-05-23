using MediatR;
using Microsoft.Extensions.Logging;
using Clients.Application.DTOs;
using Clients.Application.Interfaces;
using Clients.Domain.Entities;
using Clients.Domain.Exceptions;
using Clients.Domain.Interfaces;

namespace Clients.Application.Features.UpdateClient;

public sealed class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientResponseDto>
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientCacheService _cacheService;
    private readonly IClientEventPublisher _eventPublisher;
    private readonly IEmailService _emailService;
    private readonly ILogger<UpdateClientCommandHandler> _logger;

    public UpdateClientCommandHandler(
        IClientRepository clientRepository,
        IClientCacheService cacheService,
        IClientEventPublisher eventPublisher,
        IEmailService emailService,
        ILogger<UpdateClientCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _cacheService = cacheService;
        _eventPublisher = eventPublisher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ClientResponseDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new ClientNotFoundException(request.ClientId);

        var bankingChanged = request.BankingDetails is not null &&
            (request.BankingDetails.Agency != client.BankingDetails.Agency ||
             request.BankingDetails.AccountNumber != client.BankingDetails.AccountNumber);

        BankingDetails? newBanking = request.BankingDetails is not null
            ? new BankingDetails(request.BankingDetails.Agency, request.BankingDetails.AccountNumber)
            : null;

        client.UpdatePartial(request.Name, request.Email, request.Address, newBanking);

        await _clientRepository.UpdateAsync(client, cancellationToken);
        await _clientRepository.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidateAsync(client.Id, cancellationToken);

        if (bankingChanged)
        {
            _logger.LogInformation("Banking data changed for client {ClientId} — publishing event", client.Id);
            await _eventPublisher.PublishBankingDataUpdatedAsync(
                client.Id,
                client.BankingDetails.Agency,
                client.BankingDetails.AccountNumber,
                cancellationToken);
        }

        await _emailService.SendAsync(
            client.Email,
            "Your profile has been updated",
            $"Hello {client.Name}, your banking profile was successfully updated.",
            cancellationToken);

        var dto = new ClientResponseDto(
            client.Id,
            client.Name,
            client.Email,
            client.Address,
            client.ProfilePictureUrl,
            new BankingDetailsDto(client.BankingDetails.Agency, client.BankingDetails.AccountNumber),
            client.CreatedAt,
            client.UpdatedAt);

        return dto;
    }
}
