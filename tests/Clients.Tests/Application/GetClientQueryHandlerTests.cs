using Clients.Application.DTOs;
using Clients.Application.Features.GetClient;
using Clients.Application.Interfaces;
using Clients.Domain.Entities;
using Clients.Domain.Exceptions;
using Clients.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Clients.Tests.Application;

public class GetClientQueryHandlerTests
{
    private readonly IClientRepository _repository = Substitute.For<IClientRepository>();
    private readonly IClientCacheService _cacheService = Substitute.For<IClientCacheService>();
    private readonly ILogger<GetClientQueryHandler> _logger = Substitute.For<ILogger<GetClientQueryHandler>>();

    private GetClientQueryHandler CreateHandler() =>
        new(_repository, _cacheService, _logger);

    [Fact]
    public async Task Handle_CacheHit_ShouldReturnCachedClientWithoutHittingDatabase()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var cachedClient = new ClientResponseDto(
            clientId, "John Doe", "john@example.com", "123 Main St",
            null, new BankingDetailsDto("0001", "123456-7"), DateTime.UtcNow, null);

        _cacheService.GetAsync(clientId, Arg.Any<CancellationToken>())
            .Returns(cachedClient);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetClientQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(clientId);
        result.Name.Should().Be("John Doe");

        // Must NOT have called the database
        await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_ShouldFetchFromDatabaseAndPopulateCache()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var client = Client.Create("Jane Smith", "jane@example.com", "456 Oak Ave", "0002", "654321-0");

        _cacheService.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ClientResponseDto?)null);

        // Use reflection to set private Id for test (or expose for testing via factory)
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(client);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetClientQuery(clientId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Jane Smith");
        result.Email.Should().Be("jane@example.com");
        result.BankingDetails.Agency.Should().Be("0002");

        // Cache should have been populated
        await _cacheService.Received(1).SetAsync(Arg.Any<ClientResponseDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClientNotFoundInDatabaseAndCache_ShouldThrowClientNotFoundException()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        _cacheService.GetAsync(clientId, Arg.Any<CancellationToken>()).Returns((ClientResponseDto?)null);
        _repository.GetByIdAsync(clientId, Arg.Any<CancellationToken>()).Returns((Client?)null);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(new GetClientQuery(clientId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ClientNotFoundException>()
            .WithMessage($"*{clientId}*");
    }

    [Fact]
    public void Client_Create_WithInvalidEmail_ShouldThrowDomainException()
    {
        // Act
        var act = () => Client.Create("Test", "not-an-email", "Addr", "001", "123");

        // Assert
        act.Should().Throw<Clients.Domain.Exceptions.DomainException>()
            .WithMessage("*valid email*");
    }
}
