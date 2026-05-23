namespace Transactions.Domain.Entities;

public class Transfer
{
    public Guid Id { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid ReceiverUserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public TransferStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Transfer() { }

    public static Transfer Create(Guid senderUserId, Guid receiverUserId, decimal amount, string description)
    {
        if (senderUserId == receiverUserId)
            throw new Exceptions.DomainException("Sender and receiver cannot be the same user.");

        if (amount <= 0)
            throw new Exceptions.DomainException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new Exceptions.DomainException("Description is required.");

        return new Transfer
        {
            Id = Guid.NewGuid(),
            SenderUserId = senderUserId,
            ReceiverUserId = receiverUserId,
            Amount = amount,
            Description = description,
            Status = TransferStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        if (Status != TransferStatus.Pending)
            throw new Exceptions.DomainException("Only pending transfers can be completed.");

        Status = TransferStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        Status = TransferStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}
