namespace Transactions.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class TransferNotFoundException : Exception
{
    public TransferNotFoundException(Guid id) : base($"Transfer with id '{id}' was not found.") { }
}
