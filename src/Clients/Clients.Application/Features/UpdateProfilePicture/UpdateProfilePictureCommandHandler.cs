using MediatR;
using Microsoft.Extensions.Logging;
using Clients.Application.Interfaces;
using Clients.Domain.Exceptions;
using Clients.Domain.Interfaces;

namespace Clients.Application.Features.UpdateProfilePicture;

public sealed class UpdateProfilePictureCommandHandler : IRequestHandler<UpdateProfilePictureCommand, string>
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientCacheService _cacheService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<UpdateProfilePictureCommandHandler> _logger;

    public UpdateProfilePictureCommandHandler(
        IClientRepository clientRepository,
        IClientCacheService cacheService,
        IBlobStorageService blobStorageService,
        ILogger<UpdateProfilePictureCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _cacheService = cacheService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<string> Handle(UpdateProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new ClientNotFoundException(request.ClientId);

        _logger.LogInformation("Uploading profile picture for client {ClientId}", request.ClientId);

        var blobName = $"profile-pictures/{request.ClientId}/{Guid.NewGuid()}-{request.FileName}";
        var pictureUrl = await _blobStorageService.UploadAsync(
            request.FileStream,
            blobName,
            request.ContentType,
            cancellationToken);

        client.UpdateProfilePicture(pictureUrl);

        await _clientRepository.UpdateAsync(client, cancellationToken);
        await _clientRepository.SaveChangesAsync(cancellationToken);

        await _cacheService.InvalidateAsync(client.Id, cancellationToken);

        _logger.LogInformation("Profile picture updated for client {ClientId}. URL: {Url}", request.ClientId, pictureUrl);

        return pictureUrl;
    }
}
