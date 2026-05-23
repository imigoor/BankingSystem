namespace Clients.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class ClientNotFoundException : Exception
{
    public ClientNotFoundException(Guid id) : base($"Client with id '{id}' was not found.") { }
}
