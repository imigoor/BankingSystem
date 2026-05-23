using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Transactions.Application.Features.CreateTransfer;
using Transactions.Application.Interfaces;
using Transactions.Domain.Entities;
using Transactions.Domain.Interfaces;
using Xunit;

namespace Transactions.Tests.Application;

public class CreateTransferCommandHandlerTests
{
    private readonly ITransferRepository _repository = Substitute.For<ITransferRepository>();
    private readonly IClientServiceClient _clientService = Substitute.For<IClientServiceClient>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly ITransferEventPublisher _eventPublisher = Substitute.For<ITransferEventPublisher>();
    private readonly ILogger<CreateTransferCommandHandler> _logger = Substitute.For<ILogger<CreateTransferCommandHandler>>();

    private CreateTransferCommandHandler CreateHandler() =>
        new(_repository, _clientService, _notificationService, _eventPublisher, _logger);

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndCompleteTransfer()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var command = new CreateTransferCommand(senderId, receiverId, 500m, "Test payment");

        _clientService.ClientExistsAsync(senderId, Arg.Any<CancellationToken>()).Returns(true);
        _clientService.ClientExistsAsync(receiverId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SenderUserId.Should().Be(senderId);
        result.ReceiverUserId.Should().Be(receiverId);
        result.Amount.Should().Be(500m);
        result.Status.Should().Be("Completed");

        await _repository.Received(1).AddAsync(Arg.Any<Transfer>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Transfer>(), Arg.Any<CancellationToken>());
        await _notificationService.Received(1).SendTransferNotificationAsync(
            senderId, receiverId, 500m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SenderNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var command = new CreateTransferCommand(senderId, receiverId, 100m, "Test");

        _clientService.ClientExistsAsync(senderId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{senderId}*");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Transfer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceiverNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var command = new CreateTransferCommand(senderId, receiverId, 100m, "Test");

        _clientService.ClientExistsAsync(senderId, Arg.Any<CancellationToken>()).Returns(true);
        _clientService.ClientExistsAsync(receiverId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{receiverId}*");
    }

    [Fact]
    public void Transfer_Create_WithSameUser_ShouldThrowDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = () => Transfer.Create(userId, userId, 100m, "Self-transfer");

        // Assert
        act.Should().Throw<Transactions.Domain.Exceptions.DomainException>()
            .WithMessage("*same user*");
    }

    [Fact]
    public void Transfer_Create_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Act
        var act = () => Transfer.Create(Guid.NewGuid(), Guid.NewGuid(), -50m, "Invalid");

        // Assert
        act.Should().Throw<Transactions.Domain.Exceptions.DomainException>()
            .WithMessage("*greater than zero*");
    }
}
